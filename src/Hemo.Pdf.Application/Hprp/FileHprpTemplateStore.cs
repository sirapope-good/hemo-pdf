using System.Collections.Concurrent;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class FileHprpTemplateStore : IHprpTemplateStore
{
    private readonly string _rootPath;
    private readonly string _packagesRootPath;
    private readonly ILogger<FileHprpTemplateStore>? _logger;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<string, HprpPackage> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HprpPackage> _byVariant = new(StringComparer.OrdinalIgnoreCase);
    private long _stamp = -1;
    private bool _loaded;

    public FileHprpTemplateStore(IOptions<HprpTemplateOptions> options, ILogger<FileHprpTemplateStore>? logger = null)
    {
        var value = options.Value;
        _rootPath = HprpDiskPaths.ResolveExistingOrConfigured(value.RootPath);
        _packagesRootPath = string.IsNullOrWhiteSpace(value.PackagesRootPath)
            ? ""
            : HprpDiskPaths.ResolveExistingOrConfigured(value.PackagesRootPath);
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
            "Tenant .hprp uploads are disabled. Pack files under packages/ or edit assets/templates/reports/.");
    }

    public Task DeleteTenantOverrideAsync(
        string tenantCode,
        string templateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException(
            "Tenant .hprp uploads are disabled. Pack files under packages/ or edit assets/templates/reports/.");
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

    public IReadOnlyList<HprpPackage> ListCachedPackages()
    {
        EnsureLoaded();
        return _byVariant.Values
            .OrderBy(p => p.Manifest.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Manifest.Variant ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool HasTenantOverride(string tenantCode, string templateId) => false;

    public void Invalidate()
    {
        lock (_sync)
        {
            _loaded = false;
            _stamp = -1;
        }
    }

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
        long max = 0;
        max = MaxWriteTicks(_rootPath, max, "*.json", SearchOption.AllDirectories);
        max = MaxWriteTicks(_packagesRootPath, max, "*" + HprpEngine.FileExtension, SearchOption.TopDirectoryOnly);
        return max;
    }

    private static long MaxWriteTicks(string? root, long current, string pattern, SearchOption option)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return current;

        var max = Math.Max(current, Directory.GetLastWriteTimeUtc(root).Ticks);
        foreach (var file in Directory.EnumerateFiles(root, pattern, option))
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

        LoadPackedPackages();
        LoadUnpackedFolders();
    }

    private void LoadPackedPackages()
    {
        if (string.IsNullOrWhiteSpace(_packagesRootPath) || !Directory.Exists(_packagesRootPath))
            return;

        foreach (var zip in Directory.EnumerateFiles(_packagesRootPath, "*" + HprpEngine.FileExtension))
        {
            string? fileVariant = null;
            if (HprpTemplatePaths.TryParsePackageFileName(zip, out _, out var parsedVariant))
                fileVariant = parsedVariant;

            TryAddPackage(
                () =>
                {
                    using var stream = File.OpenRead(zip);
                    return HprpPackageReader.ReadZip(stream, zip);
                },
                zip,
                fileVariant,
                overwrite: true);
        }
    }

    private void LoadUnpackedFolders()
    {
        var scanRoot = ResolveScanRoot();
        if (!Directory.Exists(scanRoot))
        {
            if (!Directory.Exists(_packagesRootPath))
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

            TryAddPackage(() => HprpPackageReader.ReadDirectory(dir), dir, overwrite: false);
        }
    }

    private void LoadVariantPackages(string reportDir, string variantsDir)
    {
        foreach (var variantDir in Directory.EnumerateDirectories(variantsDir))
        {
            var manifestPath = Path.Combine(variantDir, HprpPackageReader.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            TryAddPackage(
                () => HprpPackageReader.ReadDirectory(variantDir),
                variantDir,
                Path.GetFileName(variantDir),
                overwrite: false);
        }

        var reportId = Path.GetFileName(reportDir);
        if (!_defaults.ContainsKey(reportId)
            && _byVariant.Values.FirstOrDefault(p =>
                string.Equals(p.Manifest.Id, reportId, StringComparison.OrdinalIgnoreCase)) is { } first)
        {
            _defaults[reportId] = first;
        }
    }

    private void TryAddPackage(
        Func<HprpPackage> load,
        string source,
        string? folderVariant = null,
        bool overwrite = true)
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

            var key = HprpTemplatePaths.CacheKey(id, variant);
            if (!overwrite && _byVariant.ContainsKey(key))
                return;

            _byVariant[key] = package;

            if (HprpTemplatePaths.IsDefaultVariant(variant))
            {
                if (overwrite || !_defaults.ContainsKey(id))
                    _defaults[id] = package;
            }
            else if (!_defaults.ContainsKey(id))
            {
                _defaults[id] = package;
            }
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
}
