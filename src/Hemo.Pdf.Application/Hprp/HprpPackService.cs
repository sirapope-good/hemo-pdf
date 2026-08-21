using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpPackResult
{
    public required string TemplateId { get; init; }
    public required string Variant { get; init; }
    public required string OutputPath { get; init; }
}

public sealed class HprpPackService
{
    private readonly HprpTemplateOptions _options;
    private readonly IHprpTemplateStore _store;

    public HprpPackService(IOptions<HprpTemplateOptions> options, IHprpTemplateStore store)
    {
        _options = options.Value;
        _store = store;
    }

    public string TemplatesRoot => HprpDiskPaths.ResolveExistingOrConfigured(_options.RootPath);

    public string PackagesReadRoot => HprpDiskPaths.ResolveExistingOrConfigured(_options.PackagesRootPath);

    public string PackagesWriteRoot => HprpDiskPaths.ResolvePackagesWriteRoot(_options);

    public HprpValidationResult Validate(HprpPackage package) => HprpValidator.Validate(package);

    public async Task<HprpPackResult> PackDirectoryAsync(
        string sourceDir,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var package = HprpPackageReader.ReadDirectory(sourceDir);
        return await WritePackageAsync(package, outputPath, cancellationToken);
    }

    public async Task<HprpPackResult> WritePackageAsync(
        HprpPackage package,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var result = HprpValidator.Validate(package);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "Invalid .hprp package: " + string.Join(" ", result.Errors));
        }

        var fullPath = Path.GetFullPath(outputPath);
        EnsureAllowedPackagePath(fullPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = File.Create(fullPath);
        await HprpPackageReader.WriteZipAsync(package, stream, cancellationToken);
        _store.Invalidate();

        return new HprpPackResult
        {
            TemplateId = package.Manifest.Id,
            Variant = HprpTemplatePaths.NormalizeVariant(package.Manifest.Variant),
            OutputPath = fullPath,
        };
    }

    public async Task<IReadOnlyList<HprpPackResult>> PackAllFromTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var templatesRoot = TemplatesRoot;
        var reportsRoot = HprpTemplatePaths.ReportsRoot(templatesRoot);
        if (!Directory.Exists(reportsRoot))
        {
            throw new DirectoryNotFoundException($"HPRP reports folder not found: {reportsRoot}");
        }

        var packagesRoot = PackagesWriteRoot;
        Directory.CreateDirectory(packagesRoot);

        var packed = new List<HprpPackResult>();
        foreach (var reportDir in Directory.EnumerateDirectories(reportsRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(reportDir);
            if (HprpTemplatePaths.IsReservedFolder(name))
                continue;

            var variantsDir = Path.Combine(reportDir, HprpTemplatePaths.VariantsFolder);
            if (Directory.Exists(variantsDir))
            {
                foreach (var variantDir in Directory.EnumerateDirectories(variantsDir))
                {
                    if (!File.Exists(Path.Combine(variantDir, HprpPackageReader.ManifestFileName)))
                        continue;

                    var variant = Path.GetFileName(variantDir);
                    packed.Add(await PackDirectoryAsync(
                        variantDir,
                        Path.Combine(
                            packagesRoot,
                            HprpTemplatePaths.PackageFileName(name, variant, includeVariantSegment: true)),
                        cancellationToken));
                }

                continue;
            }

            if (!File.Exists(Path.Combine(reportDir, HprpPackageReader.ManifestFileName)))
                continue;

            packed.Add(await PackDirectoryAsync(
                reportDir,
                Path.Combine(
                    packagesRoot,
                    HprpTemplatePaths.PackageFileName(name, variant: null, includeVariantSegment: false)),
                cancellationToken));
        }

        return packed;
    }

    public async Task<IReadOnlyList<HprpPackResult>> PackTemplateIdAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        var reportsRoot = HprpTemplatePaths.ReportsRoot(TemplatesRoot);
        var reportDir = Path.Combine(reportsRoot, templateId.Trim());
        if (!Directory.Exists(reportDir))
        {
            throw new DirectoryNotFoundException($"Template folder not found: {reportDir}");
        }

        var packagesRoot = PackagesWriteRoot;
        Directory.CreateDirectory(packagesRoot);
        var packed = new List<HprpPackResult>();
        var variantsDir = Path.Combine(reportDir, HprpTemplatePaths.VariantsFolder);
        if (Directory.Exists(variantsDir))
        {
            foreach (var variantDir in Directory.EnumerateDirectories(variantsDir))
            {
                if (!File.Exists(Path.Combine(variantDir, HprpPackageReader.ManifestFileName)))
                    continue;

                var variant = Path.GetFileName(variantDir);
                packed.Add(await PackDirectoryAsync(
                    variantDir,
                    Path.Combine(
                        packagesRoot,
                        HprpTemplatePaths.PackageFileName(templateId, variant, includeVariantSegment: true)),
                    cancellationToken));
            }
        }
        else
        {
            packed.Add(await PackDirectoryAsync(
                reportDir,
                Path.Combine(
                    packagesRoot,
                    HprpTemplatePaths.PackageFileName(templateId, variant: null, includeVariantSegment: false)),
                cancellationToken));
        }

        return packed;
    }

    public void Unpack(string zipPath, string targetDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDir);

        var info = new FileInfo(zipPath);
        if (!info.Exists)
            throw new FileNotFoundException("Packed .hprp file not found.", zipPath);
        if (info.Length > HprpEngine.MaxPackageBytes)
            throw new InvalidOperationException($"Package exceeds {HprpEngine.MaxPackageBytes} bytes.");

        using var stream = File.OpenRead(zipPath);
        var package = HprpPackageReader.ReadZip(stream, zipPath);
        Directory.CreateDirectory(targetDir);
        WriteJson(Path.Combine(targetDir, HprpPackageReader.ManifestFileName), package.Manifest);
        WriteJson(Path.Combine(targetDir, HprpPackageReader.LayoutFileName), package.Layout);
        foreach (var (language, labels) in package.LabelsByLanguage)
            WriteJson(Path.Combine(targetDir, $"labels.{language}.json"), labels);
    }

    public HprpPackage? ReadPackedFile(string templateId, string? variant)
    {
        var packagesRoot = PackagesReadRoot;
        if (!Directory.Exists(packagesRoot))
            packagesRoot = PackagesWriteRoot;
        if (!Directory.Exists(packagesRoot))
            return null;

        var includeVariant = !HprpTemplatePaths.IsDefaultVariant(variant)
            || File.Exists(Path.Combine(
                packagesRoot,
                HprpTemplatePaths.PackageFileName(templateId, variant, includeVariantSegment: true)));

        var path = Path.Combine(
            packagesRoot,
            HprpTemplatePaths.PackageFileName(templateId, variant, includeVariant));
        if (!File.Exists(path) && includeVariant)
        {
            path = Path.Combine(
                packagesRoot,
                HprpTemplatePaths.PackageFileName(templateId, variant, includeVariantSegment: false));
        }

        if (!File.Exists(path))
            return null;

        using var stream = File.OpenRead(path);
        return HprpPackageReader.ReadZip(stream, path);
    }

    public string PackageOutputPath(string templateId, string? variant, bool includeVariantSegment)
    {
        var fileName = HprpTemplatePaths.PackageFileName(templateId, variant, includeVariantSegment);
        return Path.Combine(PackagesWriteRoot, fileName);
    }

    private void EnsureAllowedPackagePath(string fullPath)
    {
        var writeRoot = Path.GetFullPath(PackagesWriteRoot);
        Directory.CreateDirectory(writeRoot);
        if (!fullPath.StartsWith(writeRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing to write .hprp outside the packages directory.");
        if (!fullPath.EndsWith(HprpEngine.FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Output must use {HprpEngine.FileExtension}.");
        if (new FileInfo(fullPath).Exists && new FileInfo(fullPath).Length > HprpEngine.MaxPackageBytes)
            throw new InvalidOperationException($"Existing package exceeds {HprpEngine.MaxPackageBytes} bytes.");
    }

    private static void WriteJson<T>(string path, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value, HprpJson.Options);
        File.WriteAllText(path, json);
    }
}
