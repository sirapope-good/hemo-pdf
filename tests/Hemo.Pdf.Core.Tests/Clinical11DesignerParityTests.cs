using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical11DesignerParityTests
{
    static Clinical11DesignerParityTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Clinical11DesignerPackage_ValidatesRendersAndSpansTwoPages()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var package = LoadClinical11Package(templatesRoot);

        var validation = HprpValidator.Validate(package);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Equal(HprpLayoutModes.Designer, package.Manifest.LayoutMode);
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.Header, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.FieldRow, StringComparison.OrdinalIgnoreCase)
                && e.Segments?.Any(s =>
                    string.Equals(s.Kind, HprpFieldRowSegmentKinds.Options, StringComparison.OrdinalIgnoreCase)) == true);
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.DataGrid, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.PageOf, StringComparison.OrdinalIgnoreCase));

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, ClinicalReportCatalog.Admission);
        Assert.NotNull(sample);
        Assert.True(sample.Value.TryGetProperty("record", out _));
        Assert.True(sample.Value.TryGetProperty("comorbid", out _));
        Assert.True(sample.Value.TryGetProperty("esrd", out _));
        Assert.True(sample.Value.TryGetProperty("access", out var accessEl));
        Assert.True(accessEl.TryGetProperty("columnHeaders", out var headersEl));
        Assert.Equal(6, headersEl.GetArrayLength());
        Assert.True(sample.Value.TryGetProperty("header", out _));

        var canvas = DesignerCanvasViewModel.FromPackage(package, sample);
        Assert.True(
            canvas.PageCount >= 2,
            $"Admission note must span at least 2 pages, got {canvas.PageCount} (flowH={canvas.ContentFlowHeightMm}mm).");

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
        var renderer = CreateDesignerRenderer(store, presets, headers);

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Admission,
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
    public async Task PackClinical11_WritesPackageFile()
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
        var packed = await pack.PackTemplateIdAsync(ClinicalReportCatalog.Admission);
        Assert.Single(packed);
        Assert.True(File.Exists(packed[0].OutputPath));
        Assert.EndsWith("clinical-11-admission.hprp", packed[0].OutputPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoPackagesRoot(string templatesRoot)
    {
        var dir = new DirectoryInfo(templatesRoot);
        DirectoryInfo? binFallback = null;
        while (dir is not null)
        {
            var packages = Path.Combine(dir.FullName, "packages");
            var hasClinical01 = File.Exists(Path.Combine(packages, "clinical-01-hct-epo.hprp"));
            var hasClinical07 = File.Exists(Path.Combine(packages, "clinical-07-lab.hprp"));
            if (hasClinical01 || hasClinical07)
            {
                var isUnderBin = packages.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase);
                if (!isUnderBin)
                    return packages;
                binFallback ??= new DirectoryInfo(packages);
            }

            dir = dir.Parent;
        }

        if (binFallback is not null)
            return binFallback.FullName;

        throw new DirectoryNotFoundException("repo packages/ folder not found from " + templatesRoot);
    }

    private static HprpPackage LoadClinical11Package(string templatesRoot)
    {
        var dir = Path.Combine(templatesRoot, "reports", ClinicalReportCatalog.Admission);
        return HprpPackageReader.ReadDirectory(dir);
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
