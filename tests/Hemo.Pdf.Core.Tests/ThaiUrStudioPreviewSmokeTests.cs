using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class ThaiUrStudioPreviewSmokeTests
{
    static ThaiUrStudioPreviewSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task SamplePayload_ThaiUrDenseForm_FitsOnePage()
    {
        var root = HprpTestAssets.TemplatesRoot();
        var samplePath = Path.Combine(root, "reports", ClinicalReportCatalog.HemodialysisRecord, "sample.json");
        Assert.True(File.Exists(samplePath), samplePath);

        var package = HprpPackageReader.ReadDirectory(
            Path.Combine(root, "reports", ClinicalReportCatalog.HemodialysisRecord, "variants", "thaiur"));

        using var doc = JsonDocument.Parse(await File.ReadAllBytesAsync(samplePath));
        var data = doc.RootElement.Clone();

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HemodialysisRecord,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = "Hemodialysis Record" },
            Data = data,
            LayoutPackage = package,
            Branding = new CustomerBrandingProfile
            {
                Style = new BrandingStyle { SectionHeaderBackground = "#384BA8" },
            },
        };

        var dataProvider = new HemosheetDataProvider();
        var model = await dataProvider.GetDataAsync(context, CancellationToken.None);
        var vm = Assert.IsType<HemosheetReportViewModel>(model);
        Assert.Equal(HemosheetLayoutProfile.ThaiUr, vm.LayoutContext.LayoutProfile);

        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var form = new ThaiUrHemosheetForm();
        var layout = new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => form.Compose(c, vm, context, package),
            Footer = null,
            SectionHeaderBackground = ReportSectionHeaderChrome.FromBranding(context.Branding),
        };

        var bytes = await new QuestPdfRenderer().RenderAsync(layout, CancellationToken.None);

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        var pages = CountPdfPages(bytes);
        Assert.True(pages == 1, $"Studio sample ThaiUr should fit 1 page, got {pages} (bytes={bytes.Length}).");
    }

    private static int CountPdfPages(byte[] pdf)
    {
        var text = System.Text.Encoding.Latin1.GetString(pdf);
        return System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
    }
}
