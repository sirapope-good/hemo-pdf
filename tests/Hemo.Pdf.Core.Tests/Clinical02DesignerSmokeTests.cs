using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;
using Hemo.Pdf.Rendering;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical02DesignerSmokeTests
{
    static Clinical02DesignerSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DesignerPackage_ValidatesAndRendersPdf()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templatesRoot,
            PackagesRootPath = Path.Combine(templatesRoot, "_no-packages"),
        });
        var store = new FileHprpTemplateStore(options);
        var presetStore = new HprpTablePresetStore(options);
        var presets = new HprpTablePresetCatalog(presetStore);
        var headerStore = new HprpHeaderPresetStore(options);
        var headers = new HprpHeaderPresetCatalog(headerStore);
        var pack = new HprpPackService(options, store);
        var package = LoadPackage(templatesRoot);

        var validation = pack.Validate(package);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.True(HprpLayoutModes.IsDesigner(package.Manifest));

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, ClinicalReportCatalog.EpoDrug);
        Assert.NotNull(sample);

        var renderer = new Clinical02EpoDrugReportRenderer(
            new Clinical02EpoDrugDataProvider(),
            new Clinical02EpoDrugComposer(store, presets, headers),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.EpoDrug,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = package.Manifest.DisplayName },
            Data = sample.Value.Clone(),
            LayoutPackage = package,
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public async Task PackClinical02_WritesPackageFile()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var packages = FindRepoPackagesRoot(templatesRoot);
        Directory.CreateDirectory(packages);
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templatesRoot,
            PackagesRootPath = packages,
            PackagesWritePath = packages,
            EnableHprpStudioWrite = true,
        });
        var store = new FileHprpTemplateStore(options);
        var pack = new HprpPackService(options, store);
        var packed = await pack.PackTemplateIdAsync(ClinicalReportCatalog.EpoDrug);
        Assert.Single(packed);
        Assert.True(File.Exists(packed[0].OutputPath));
        Assert.Equal(
            Path.GetFullPath(Path.Combine(packages, "clinical-02-epo-drug.hprp")),
            Path.GetFullPath(packed[0].OutputPath),
            ignoreCase: true);
        Assert.True(new FileInfo(packed[0].OutputPath).Length > 1500);
    }

    private static string FindRepoPackagesRoot(string templatesRoot)
    {
        var repo = HprpTemplatePaths.FindRepoRoot(templatesRoot, Directory.GetCurrentDirectory());
        if (!string.IsNullOrWhiteSpace(repo))
        {
            var packages = Path.Combine(repo, HprpTemplatePaths.PackagesFolder);
            if (Directory.Exists(packages))
                return packages;
        }

        throw new DirectoryNotFoundException("repo packages/ folder not found from " + templatesRoot);
    }

    private static HprpPackage LoadPackage(string templatesRoot)
    {
        var dir = Path.Combine(templatesRoot, "reports", ClinicalReportCatalog.EpoDrug);
        return new HprpPackage
        {
            Manifest = JsonSerializer.Deserialize<HprpManifest>(
                File.ReadAllText(Path.Combine(dir, "manifest.json")),
                HprpJson.Options)!,
            Layout = JsonSerializer.Deserialize<HprpLayout>(
                File.ReadAllText(Path.Combine(dir, "layout.json")),
                HprpJson.Options)!,
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["th"] = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Path.Combine(dir, "labels.th.json")),
                    HprpJson.Options)!,
            },
        };
    }
}
