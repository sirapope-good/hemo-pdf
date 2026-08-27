using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Absolute;
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
