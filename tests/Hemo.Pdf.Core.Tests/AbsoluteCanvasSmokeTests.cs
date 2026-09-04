using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Absolute;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class AbsoluteCanvasSmokeTests
{
    public AbsoluteCanvasSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Validator_AcceptsAbsoluteDemoPackage()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "experimental-absolute-demo");
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.True(HprpLayoutModes.IsAbsolute(package.Manifest));
        var result = HprpValidator.Validate(package);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotEmpty(package.Layout.Widgets);
    }

    [Fact]
    public void Validator_AcceptsAbsoluteClinical01Package()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "experimental-absolute-clinical-01");
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.True(HprpLayoutModes.IsAbsolute(package.Manifest));
        Assert.Equal(HprpDataAdapterIds.Clinical01HctEpo, package.Manifest.DataAdapter);
        Assert.All(package.Layout.Widgets, w => Assert.Equal(HprpAbsoluteWidget.TypeDense, w.Type));
        var result = HprpValidator.Validate(package);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CompositionPackages_StillValidateWithoutLayoutMode()
    {
        var dir = HprpTestAssets.PackageDir(ClinicalReportCatalog.Prescription);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.False(HprpLayoutModes.IsAbsolute(package.Manifest));
        Assert.True(HprpValidator.Validate(package).IsValid);
    }

    [Fact]
    public async Task AbsoluteDemo_RendersNonEmptyPdf()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "experimental-absolute-demo");
        var package = HprpPackageReader.ReadDirectory(dir);
        var vm = AbsoluteCanvasViewModel.FromPackage(package);
        var layout = AbsoluteCanvasComposer.Compose(vm, new PdfReportContext
        {
            ReportTemplateId = package.Manifest.Id,
            TenantCode = "local",
            LayoutPackage = package,
        });

        var renderer = new QuestPdfRenderer();
        var pdf = await renderer.RenderAsync(layout, CancellationToken.None);
        Assert.True(pdf.Length > 500);
        Assert.Equal(0x25, pdf[0]); // %PDF
    }

    [Fact]
    public async Task AbsoluteClinical01_RendersNonEmptyPdf()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "experimental-absolute-clinical-01");
        var package = HprpPackageReader.ReadDirectory(dir);
        var samplePath = Path.Combine(dir, "sample.json");
        await using var sampleStream = File.OpenRead(samplePath);
        using var sampleDoc = await JsonDocument.ParseAsync(sampleStream);

        var context = new PdfReportContext
        {
            ReportTemplateId = package.Manifest.Id,
            TenantCode = "local",
            LayoutPackage = package,
            Data = sampleDoc.RootElement.Clone(),
        };

        var bound = await new Clinical01HctEpoDataProvider().GetDataAsync(context, CancellationToken.None);
        var vm = AbsoluteCanvasViewModel.FromPackage(package, bound, package.GetLabels("th"));
        var layout = AbsoluteCanvasComposer.Compose(vm, context);

        var renderer = new QuestPdfRenderer();
        var pdf = await renderer.RenderAsync(layout, CancellationToken.None);
        Assert.True(pdf.Length > 2000);
        Assert.Equal(0x25, pdf[0]);
    }

    [Fact]
    public void AbsoluteDense_BudgetsMonthRowsFromBoxHeight()
    {
        var rowH = AbsoluteDenseWidgetHost.BudgetMonthRowHeightFromBoxMm(228f);
        Assert.True(rowH >= 12f);
        Assert.True(rowH < 30f);
    }

    [Fact]
    public async Task AbsoluteDemo_ZipRoundTrip()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "experimental-absolute-demo");
        var package = HprpPackageReader.ReadDirectory(dir);

        await using var ms = new MemoryStream();
        await HprpPackageReader.WriteZipAsync(package, ms, CancellationToken.None);
        ms.Position = 0;
        var loaded = HprpPackageReader.ReadZip(ms, "experimental-absolute-demo.hprp");

        Assert.True(HprpLayoutModes.IsAbsolute(loaded.Manifest));
        Assert.True(HprpValidator.Validate(loaded).IsValid);
        Assert.Equal(4, loaded.Layout.Widgets.Count);
    }
}
