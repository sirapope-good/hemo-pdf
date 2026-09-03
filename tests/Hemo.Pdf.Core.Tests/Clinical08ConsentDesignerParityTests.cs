using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Rendering;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical08ConsentDesignerParityTests
{
    static Clinical08ConsentDesignerParityTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.ConsentTh)]
    [InlineData(ClinicalReportCatalog.ConsentEn)]
    public async Task ConsentDesignerPackage_ValidatesAndRendersPdf(string templateId)
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var package = LoadConsentPackage(templatesRoot, templateId);

        var validation = HprpValidator.Validate(package);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Equal(HprpLayoutModes.Designer, package.Manifest.LayoutMode);
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.Header, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.Dense, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Widget, HprpWidgetIds.ClinicalConsentNarrative, StringComparison.OrdinalIgnoreCase));

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, templateId);
        Assert.NotNull(sample);

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

        var renderer = new ConsentReportRenderer(
            new ConsentReportDataProvider(),
            new ConsentReportComposer(store, presets, headers),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = templateId,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = package.Manifest.DisplayName },
            Data = sample.Value.Clone(),
            LayoutPackage = package,
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.ConsentTh)]
    [InlineData(ClinicalReportCatalog.ConsentEn)]
    public async Task PackConsent_WritesPackageFile(string templateId)
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var packages = Path.GetFullPath(Path.Combine(templatesRoot, "..", "..", "..", "..", "..", "packages"));
        if (!Directory.Exists(packages) || !File.Exists(Path.Combine(packages, "clinical-07-lab.hprp")))
        {
            packages = Path.GetFullPath(Path.Combine(templatesRoot, "..", "..", "packages"));
        }

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
        var packed = await pack.PackTemplateIdAsync(templateId);
        Assert.Single(packed);
        Assert.True(File.Exists(packed[0].OutputPath));
        Assert.EndsWith($"{templateId}.hprp", packed[0].OutputPath, StringComparison.OrdinalIgnoreCase);
    }

    private static HprpPackage LoadConsentPackage(string templatesRoot, string templateId)
    {
        var dir = Path.Combine(templatesRoot, "reports", templateId);
        var labelsFile = File.Exists(Path.Combine(dir, "labels.th.json"))
            ? "labels.th.json"
            : "labels.en.json";
        var lang = labelsFile.Contains(".th.", StringComparison.Ordinal) ? "th" : "en";
        return new HprpPackage
        {
            Manifest = JsonSerializer.Deserialize<HprpManifest>(
                File.ReadAllText(Path.Combine(dir, HprpPackageReader.ManifestFileName)),
                HprpJson.Options)!,
            Layout = JsonSerializer.Deserialize<HprpLayout>(
                File.ReadAllText(Path.Combine(dir, HprpPackageReader.LayoutFileName)),
                HprpJson.Options)!,
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [lang] = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Path.Combine(dir, labelsFile)),
                    HprpJson.Options)!,
            },
        };
    }
}
