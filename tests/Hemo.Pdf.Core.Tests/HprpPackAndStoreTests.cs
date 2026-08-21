using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Core.Tests;

public class HprpPackAndStoreTests
{
    [Theory]
    [InlineData("clinical-01-hct-epo.hprp", "clinical-01-hct-epo", "default")]
    [InlineData("clinical-03-hemodialysis-record.rama.hprp", "clinical-03-hemodialysis-record", "rama")]
    [InlineData("clinical-03-hemodialysis-record.default.hprp", "clinical-03-hemodialysis-record", "default")]
    public void ParsePackageFileName(string fileName, string id, string variant)
    {
        Assert.True(HprpTemplatePaths.TryParsePackageFileName(fileName, out var parsedId, out var parsedVariant));
        Assert.Equal(id, parsedId);
        Assert.Equal(variant, parsedVariant);
    }

    [Fact]
    public void PackageFileName_SingleAndVariant()
    {
        Assert.Equal(
            "clinical-01-hct-epo.hprp",
            HprpTemplatePaths.PackageFileName("clinical-01-hct-epo", null, includeVariantSegment: false));
        Assert.Equal(
            "clinical-03-hemodialysis-record.rama.hprp",
            HprpTemplatePaths.PackageFileName(
                "clinical-03-hemodialysis-record",
                "rama",
                includeVariantSegment: true));
    }

    [Fact]
    public async Task PackDirectory_RoundTripsThroughZip()
    {
        var temp = NewTemp("roundtrip");
        var output = Path.Combine(temp, "clinical-01-hct-epo.hprp");
        var (pack, _) = CreatePack(temp);

        var result = await pack.PackDirectoryAsync(HprpTestAssets.PackageDir(ClinicalReportCatalog.HctEpo), output);
        Assert.True(File.Exists(result.OutputPath));
        Assert.True(new FileInfo(result.OutputPath).Length < HprpEngine.MaxPackageBytes);

        using var stream = File.OpenRead(output);
        var loaded = HprpPackageReader.ReadZip(stream, output);
        Assert.Equal(ClinicalReportCatalog.HctEpo, loaded.Manifest.Id);
        Assert.Equal("thaiur.header", loaded.Layout.Header?.Widget);
        Assert.Contains(loaded.Layout.Body, n => n.Widget == "clinical.hct-epo-annual-table");
        Assert.True(HprpValidator.Validate(loaded).IsValid);
    }

    [Fact]
    public async Task PackAll_CreatesSingleReportsAndHemosheetVariants()
    {
        var temp = NewTemp("packall");
        var (pack, _) = CreatePack(temp);

        var results = await pack.PackAllFromTemplatesAsync();
        Assert.True(results.Count >= 18, $"expected all reports + hemosheet variants, got {results.Count}");
        Assert.Contains(results, r => r.TemplateId == ClinicalReportCatalog.HctEpo && r.Variant == "default");
        Assert.Contains(results, r => r.TemplateId == ClinicalReportCatalog.HemodialysisRecord && r.Variant == "default");
        Assert.Contains(results, r => r.TemplateId == ClinicalReportCatalog.HemodialysisRecord && r.Variant == "rama");
        Assert.Contains(results, r => r.TemplateId == ClinicalReportCatalog.HemodialysisRecord && r.Variant == "thaiur");
        Assert.True(File.Exists(Path.Combine(temp, "clinical-01-hct-epo.hprp")));
        Assert.True(File.Exists(Path.Combine(temp, "clinical-03-hemodialysis-record.rama.hprp")));
    }

    [Fact]
    public async Task Store_PrefersPackedHprpOverFolder()
    {
        var temp = NewTemp("prefer");
        var templates = Path.Combine(temp, "templates");
        var packages = Path.Combine(temp, "packages");
        CopyDirectory(HprpTestAssets.TemplatesRoot(), templates);
        Directory.CreateDirectory(packages);

        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templates,
            PackagesRootPath = packages,
            PackagesWritePath = packages,
        });
        var store = new FileHprpTemplateStore(options);
        var pack = new HprpPackService(options, store);
        await pack.PackTemplateIdAsync(ClinicalReportCatalog.HctEpo);

        var manifestPath = Path.Combine(
            templates,
            HprpTemplatePaths.ReportsFolder,
            ClinicalReportCatalog.HctEpo,
            HprpPackageReader.ManifestFileName);
        var json = File.ReadAllText(manifestPath).Replace(
            "Hemodialysis Review Hct and EPO",
            "FROM FOLDER ONLY");
        File.WriteAllText(manifestPath, json);

        store.Invalidate();
        var loaded = store.TryGetCached("local", ClinicalReportCatalog.HctEpo);
        Assert.Equal("Hemodialysis Review Hct and EPO", loaded!.Manifest.DisplayName);
        Assert.EndsWith(".hprp", loaded.SourcePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WritePackage_RejectsUnknownWidget()
    {
        var temp = NewTemp("invalid");
        var (pack, _) = CreatePack(temp);
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-99-demo",
                DisplayName = "Demo",
                EngineVersion = 1,
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body = [new HprpLayoutNode { Widget = "not-a-real-widget" }],
            },
        };

        var validation = pack.Validate(package);
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("unknown widget", StringComparison.OrdinalIgnoreCase));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pack.WritePackageAsync(package, Path.Combine(temp, "clinical-99-demo.hprp")));
        Assert.False(File.Exists(Path.Combine(temp, "clinical-99-demo.hprp")));
    }

    [Fact]
    public async Task WritePackage_RejectsNewerEngineVersion()
    {
        var temp = NewTemp("engine");
        var (pack, _) = CreatePack(temp);
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-99-demo",
                DisplayName = "Demo",
                EngineVersion = HprpEngine.CurrentVersion + 1,
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body = [new HprpLayoutNode { Type = "text" }],
            },
        };

        Assert.False(pack.Validate(package).IsValid);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pack.WritePackageAsync(package, Path.Combine(temp, "clinical-99-demo.hprp")));
        Assert.False(File.Exists(Path.Combine(temp, "clinical-99-demo.hprp")));
    }

    [Fact]
    public async Task PackAll_SeedsRepoPackagesDirectory()
    {
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
        });
        var store = new FileHprpTemplateStore(Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = Path.Combine(Path.GetTempPath(), "hprp-none-" + Guid.NewGuid().ToString("N")),
        }));
        var pack = new HprpPackService(options, store);
        var results = await pack.PackAllFromTemplatesAsync();

        Assert.True(results.Count >= 18);
        Assert.True(Directory.Exists(pack.PackagesWriteRoot));
        Assert.True(File.Exists(Path.Combine(pack.PackagesWriteRoot, "clinical-01-hct-epo.hprp")));
        Assert.True(File.Exists(Path.Combine(pack.PackagesWriteRoot, "clinical-03-hemodialysis-record.thaiur.hprp")));
    }

    private static (HprpPackService Pack, FileHprpTemplateStore Store) CreatePack(string packagesDir)
    {
        Directory.CreateDirectory(packagesDir);
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = packagesDir,
            PackagesWritePath = packagesDir,
        });
        var store = new FileHprpTemplateStore(options);
        return (new HprpPackService(options, store), store);
    }

    private static string NewTemp(string suffix)
    {
        var dir = Path.Combine(Path.GetTempPath(), "hprp-" + suffix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, target));

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var dest = file.Replace(source, target);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
