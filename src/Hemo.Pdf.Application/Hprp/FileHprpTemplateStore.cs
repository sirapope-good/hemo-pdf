using System.Collections.Concurrent;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class FileHprpTemplateStore : IHprpTemplateStore
{
    private readonly string _rootPath;
    private readonly ILogger<FileHprpTemplateStore>? _logger;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<string, HprpPackage> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HprpPackage> _byVariant = new(StringComparer.OrdinalIgnoreCase);
    private long _stamp = -1;
    private bool _loaded;

    public FileHprpTemplateStore(IOptions<HprpTemplateOptions> options, ILogger<FileHprpTemplateStore>? logger = null)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
        _logger = logger;
        EnsureLoaded();
    }

    public HprpPackage? TryGetCached(string tenantCode, string templateId, string? variant = null)
    {
        EnsureLoaded();
        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var key = HprpTemplatePaths.CacheKey(id, variant);
        if (_byVariant.TryGetValue(key, out var package))
            return package;

        return _defaults.TryGetValue(id, out var fallback) ? fallback : null;
    }

    public Task<HprpPackage?> GetAsync(string tenantCode, string templateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TryGetCached(tenantCode, templateId));
    }

    public Task SaveTenantOverrideAsync(
        string tenantCode,
        string templateId,
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException(
            "Tenant .hprp uploads are disabled. Add a variant folder under assets/templates/reports/.");
    }

    public Task DeleteTenantOverrideAsync(
        string tenantCode,
        string templateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException(
            "Tenant .hprp uploads are disabled. Add a variant folder under assets/templates/reports/.");
    }

    public IReadOnlyList<HprpManifest> ListDefaultManifests()
    {
        EnsureLoaded();
        return _defaults.Values
            .Select(p => p.Manifest)
            .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<HprpManifest> ListLayoutProfiles(string? role = null)
    {
        EnsureLoaded();
        var target = string.IsNullOrWhiteSpace(role)
            ? HprpManifestUi.RoleHemosheetLayoutProfile
            : role.Trim();

        return _byVariant.Values
            .Select(p => p.Manifest)
            .Where(m => string.Equals(m.Ui?.Role, target, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Ui?.SortOrder ?? 0)
            .ThenBy(m => m.Variant ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool HasTenantOverride(string tenantCode, string templateId) => false;

    private void EnsureLoaded()
    {
        var stamp = ComputeStamp();
        lock (_sync)
        {
            if (_loaded && stamp == _stamp)
                return;

            ReloadUnlocked();
            _stamp = stamp;
            _loaded = true;
        }
    }

    private long ComputeStamp()
    {
        var scanRoot = ResolveScanRoot();
        if (!Directory.Exists(scanRoot))
            return 0;

        long max = Directory.GetLastWriteTimeUtc(scanRoot).Ticks;
        foreach (var file in Directory.EnumerateFiles(scanRoot, "*.json", SearchOption.AllDirectories))
        {
            var ticks = File.GetLastWriteTimeUtc(file).Ticks;
            if (ticks > max)
                max = ticks;
        }

        return max;
    }

    private void ReloadUnlocked()
    {
        _defaults.Clear();
        _byVariant.Clear();

        var scanRoot = ResolveScanRoot();
        if (!Directory.Exists(scanRoot))
        {
            _logger?.LogWarning("HPRP templates root not found: {Path}", _rootPath);
            return;
        }

        foreach (var dir in Directory.EnumerateDirectories(scanRoot))
        {
            var name = Path.GetFileName(dir);
            if (HprpTemplatePaths.IsReservedFolder(name))
                continue;

            var variantsDir = Path.Combine(dir, HprpTemplatePaths.VariantsFolder);
            if (Directory.Exists(variantsDir))
            {
                LoadVariantPackages(dir, variantsDir);
                continue;
            }

            var manifestPath = Path.Combine(dir, HprpPackageReader.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            TryAddPackage(() => HprpPackageReader.ReadDirectory(dir), dir);
        }

        foreach (var zip in Directory.EnumerateFiles(scanRoot, "*" + HprpEngine.FileExtension))
        {
            TryAddPackage(
                () =>
                {
                    using var stream = File.OpenRead(zip);
                    return HprpPackageReader.ReadZip(stream, zip);
                },
                zip);
        }
    }

    private void LoadVariantPackages(string reportDir, string variantsDir)
    {
        foreach (var variantDir in Directory.EnumerateDirectories(variantsDir))
        {
            var manifestPath = Path.Combine(variantDir, HprpPackageReader.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            TryAddPackage(() => HprpPackageReader.ReadDirectory(variantDir), variantDir, Path.GetFileName(variantDir));
        }

        var reportId = Path.GetFileName(reportDir);
        if (!_defaults.ContainsKey(reportId)
            && _byVariant.Values.FirstOrDefault(p =>
                string.Equals(p.Manifest.Id, reportId, StringComparison.OrdinalIgnoreCase)) is { } first)
        {
            _defaults[reportId] = first;
        }
    }

    private void TryAddPackage(Func<HprpPackage> load, string source, string? folderVariant = null)
    {
        try
        {
            var package = load();
            var id = package.Manifest.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger?.LogWarning("Skipped HPRP package with empty id at {Source}", source);
                return;
            }

            var variant = HprpTemplatePaths.NormalizeVariant(
                !string.IsNullOrWhiteSpace(package.Manifest.Variant)
                    ? package.Manifest.Variant
                    : folderVariant);

            _byVariant[HprpTemplatePaths.CacheKey(id, variant)] = package;

            if (HprpTemplatePaths.IsDefaultVariant(variant) || !_defaults.ContainsKey(id))
                _defaults[id] = package;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Skipped invalid HPRP package {Source}", source);
        }
    }

    private string ResolveScanRoot()
    {
        var reports = HprpTemplatePaths.ReportsRoot(_rootPath);
        return Directory.Exists(reports) ? reports : _rootPath;
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
