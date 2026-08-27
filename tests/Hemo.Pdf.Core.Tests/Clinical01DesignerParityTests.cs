using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical01DesignerParityTests
{
    static Clinical01DesignerParityTests()
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
        var package = LoadDesignerPackage(templatesRoot);

        var validation = pack.Validate(package);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, "clinical-01-hct-epo-designer");
        Assert.NotNull(sample);

        var renderer = CreateDesignerRenderer(store, presets, headers);
        var context = new PdfReportContext
        {
            ReportTemplateId = "clinical-01-hct-epo-designer",
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
    public async Task DesignerVsComposition_PdfSizeWithinTolerance()
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

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, ClinicalReportCatalog.HctEpo);
        Assert.NotNull(sample);
        var data = sample.Value.Clone();

        var compositionRenderer = new Clinical01HctEpoReportRenderer(
            new Clinical01HctEpoDataProvider(),
            new Clinical01HctEpoComposer(store, presets, headers),
            new QuestPdfRenderer());

        var compositionBytes = await compositionRenderer.RenderReportAsync(
            new PdfReportContext
            {
                ReportTemplateId = ClinicalReportCatalog.HctEpo,
                TenantCode = "local",
                Metadata = new ReportMetadata { Title = "Composition" },
                Data = data,
            },
            CancellationToken.None);

        var designerRenderer = CreateDesignerRenderer(store, presets, headers);
        var designerBytes = await designerRenderer.RenderReportAsync(
            new PdfReportContext
            {
                ReportTemplateId = "clinical-01-hct-epo-designer",
                TenantCode = "local",
                Metadata = new ReportMetadata { Title = "Designer" },
                Data = data,
                LayoutPackage = LoadDesignerPackage(templatesRoot),
            },
            CancellationToken.None);

        var ratio = (double)designerBytes.Length / compositionBytes.Length;
        Assert.InRange(ratio, 0.75, 1.35);
    }

    [Fact]
    public async Task PackDesigner_WritesPackageFile()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var packages = Path.GetFullPath(Path.Combine(templatesRoot, "..", "..", "packages"));
        Directory.CreateDirectory(packages);
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templatesRoot,
            PackagesRootPath = packages,
            PackagesWritePath = packages,
        });
        var store = new FileHprpTemplateStore(options);
        var pack = new HprpPackService(options, store);
        var packed = await pack.PackTemplateIdAsync("clinical-01-hct-epo-designer");
        Assert.Single(packed);
        var output = packed[0].OutputPath;
        Assert.True(File.Exists(output));
        Assert.EndsWith("clinical-01-hct-epo-designer.hprp", output, StringComparison.OrdinalIgnoreCase);
    }

    private static HprpPackage LoadDesignerPackage(string templatesRoot)
    {
        var designerDir = Path.Combine(templatesRoot, "reports", "clinical-01-hct-epo-designer");
        return new HprpPackage
        {
            Manifest = JsonSerializer.Deserialize<HprpManifest>(
                File.ReadAllText(Path.Combine(designerDir, "manifest.json")),
                HprpJson.Options)!,
            Layout = JsonSerializer.Deserialize<HprpLayout>(
                File.ReadAllText(Path.Combine(designerDir, "layout.json")),
                HprpJson.Options)!,
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["th"] = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Path.Combine(designerDir, "labels.th.json")),
                    HprpJson.Options)!,
            },
        };
    }

    private static ClinicalDefaultReportRenderer CreateDesignerRenderer(
        FileHprpTemplateStore store,
        HprpTablePresetCatalog presets,
        HprpHeaderPresetCatalog headers)
    {
        var composer = new ClinicalDefaultComposer(
            new FixedSectionResolver<IReportHeaderSection>(new EmptyHeaderSection()),
            new FixedSectionResolver<IReportFooterSection>(new EmptyFooterSection()));

        return new ClinicalDefaultReportRenderer(
            new ClinicalDefaultDataProvider(store, new Clinical01HctEpoDataProvider(), presets, headers),
            composer,
            new QuestPdfRenderer());
    }

    private sealed class FixedSectionResolver<T>(T section) : ISectionResolver<T>
        where T : notnull
    {
        public T Resolve(PdfReportContext context) => section;
    }

    private sealed class EmptyHeaderSection : IReportHeaderSection
    {
        public void Compose(IContainer container, object data, PdfReportContext context) { }
    }

    private sealed class EmptyFooterSection : IReportFooterSection
    {
        public void Compose(IContainer container, object data, PdfReportContext context) { }
    }
}
