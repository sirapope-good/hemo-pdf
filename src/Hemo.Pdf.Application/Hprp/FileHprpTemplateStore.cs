using System.Collections.Concurrent;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class FileHprpTemplateStore : IHprpTemplateStore
{
    private static readonly Dictionary<string, string> TenantAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["localhost"] = "local",
        ["127.0.0.1"] = "local",
    };

    private readonly string _rootPath;
    private readonly ILogger<FileHprpTemplateStore>? _logger;
    private readonly ConcurrentDictionary<string, HprpPackage> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HprpPackage> _tenantCache = new(StringComparer.OrdinalIgnoreCase);

    public FileHprpTemplateStore(IOptions<HprpTemplateOptions> options, ILogger<FileHprpTemplateStore>? logger = null)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
        _logger = logger;
        LoadDefaults();
    }

    public HprpPackage? TryGetCached(string tenantCode, string templateId)
    {
        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var tenant = NormalizeTenant(tenantCode);
        if (!string.IsNullOrWhiteSpace(tenant)
            && _tenantCache.TryGetValue(TenantKey(tenant, id), out var overlay))
        {
            return overlay;
        }

        var overlayPath = TenantPackagePath(tenant, id);
        if (!string.IsNullOrWhiteSpace(tenant) && File.Exists(overlayPath))
        {
            try
            {
                using var stream = File.OpenRead(overlayPath);
                var package = HprpPackageReader.ReadZip(stream, overlayPath);
                _tenantCache[TenantKey(tenant, id)] = package;
                return package;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Invalid tenant .hprp override for {Tenant} {Template}; using default.", tenant, id);
            }
        }

        return _defaults.TryGetValue(id, out var fallback) ? fallback : null;
    }

    public Task<HprpPackage?> GetAsync(string tenantCode, string templateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TryGetCached(tenantCode, templateId));
    }

    public async Task SaveTenantOverrideAsync(
        string tenantCode,
        string templateId,
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        var tenant = NormalizeTenant(tenantCode);
        if (string.IsNullOrWhiteSpace(tenant))
            throw new ArgumentException("Tenant code is required.", nameof(tenantCode));

        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        await using var buffer = new MemoryStream();
        await zipStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var package = HprpPackageReader.ReadZip(buffer, $"{tenant}/{id}{HprpEngine.FileExtension}");
        if (!string.Equals(package.Manifest.Id, id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package id '{package.Manifest.Id}' does not match template '{id}'.");
        }

        var path = TenantPackagePath(tenant, id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        buffer.Position = 0;
        await using (var file = File.Create(path))
        {
            await buffer.CopyToAsync(file, cancellationToken);
        }

        _tenantCache[TenantKey(tenant, id)] = package;
        _logger?.LogInformation("Saved tenant .hprp override {Path}", path);
    }

    public IReadOnlyList<HprpManifest> ListDefaultManifests() =>
        _defaults.Values.Select(p => p.Manifest).OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase).ToList();

    public bool HasTenantOverride(string tenantCode, string templateId)
    {
        var tenant = NormalizeTenant(tenantCode);
        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        return !string.IsNullOrWhiteSpace(tenant) && File.Exists(TenantPackagePath(tenant, id));
    }

    private void LoadDefaults()
    {
        if (!Directory.Exists(_rootPath))
        {
            _logger?.LogWarning("HPRP templates root not found: {Path}", _rootPath);
            return;
        }

        foreach (var dir in Directory.EnumerateDirectories(_rootPath))
        {
            var name = Path.GetFileName(dir);
            if (string.Equals(name, "tenants", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "schema", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "_shared", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var manifestPath = Path.Combine(dir, HprpPackageReader.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var package = HprpPackageReader.ReadDirectory(dir);
                _defaults[package.Manifest.Id] = package;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Skipped invalid default template folder {Dir}", dir);
            }
        }

        foreach (var zip in Directory.EnumerateFiles(_rootPath, "*" + HprpEngine.FileExtension))
        {
            try
            {
                using var stream = File.OpenRead(zip);
                var package = HprpPackageReader.ReadZip(stream, zip);
                _defaults.TryAdd(package.Manifest.Id, package);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Skipped invalid default .hprp {File}", zip);
            }
        }
    }

    private string TenantPackagePath(string tenant, string templateId) =>
        Path.Combine(_rootPath, "tenants", tenant, templateId + HprpEngine.FileExtension);

    private static string TenantKey(string tenant, string templateId) => $"{tenant}/{templateId}";

    private static string NormalizeTenant(string? tenantCode)
    {
        var tenant = tenantCode?.Trim() ?? "";
        if (TenantAliases.TryGetValue(tenant, out var alias))
            return alias;
        return tenant;
    }

    private static string ResolveRootPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath)),
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}
