using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class ThaiUrMinPageTests
{
    static ThaiUrMinPageTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task Pages_ForDialysisCount(int dialysisCount)
    {
        var root = FindTemplatesRoot();
        var package = HprpPackageReader.ReadDirectory(
            Path.Combine(root, "reports", ClinicalReportCatalog.HemodialysisRecord, "variants", "thaiur"));

        var vm = new HemosheetReportViewModel
        {
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = HemosheetLayoutProfile.ThaiUr,
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    ShowDateAndHdNo = true,
                    FixedLines = new HemosheetFixedLinesViewModel
                    {
                        Dialysis = dialysisCount,
                        Nurse = 2,
                        Medicine = 2,
                    },
                },
                Features = new Dictionary<string, bool> { ["showAvPanel"] = true },
            },
            DialysisRecords = Enumerable.Range(0, dialysisCount)
                .Select(_ => new HemosheetDialysisRecordViewModel())
                .ToList(),
            Patient = new HemosheetPatientViewModel { Name = "Test", Hn = "1" },
            Unit = new HemosheetUnitViewModel { FullName = "TRD" },
        };

        var ctx = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HemodialysisRecord,
            TenantCode = "local",
            Metadata = new ReportMetadata(),
            LayoutPackage = package,
            Branding = new CustomerBrandingProfile
            {
                Style = new BrandingStyle { SectionHeaderBackground = "#384BA8" },
            },
        };

        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var form = new ThaiUrHemosheetForm();
        var layout = new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Content = c => form.Compose(c, vm, ctx, package),
            SectionHeaderBackground = ReportSectionHeaderChrome.FromBranding(ctx.Branding),
        };

        var bytes = await new QuestPdfRenderer().RenderAsync(layout, CancellationToken.None);
        var pages = System.Text.RegularExpressions.Regex
            .Matches(System.Text.Encoding.Latin1.GetString(bytes), @"/Type\s*/Page[^s]")
            .Count;

        Assert.True(pages == 1, $"dialysis={dialysisCount} pages={pages} bytes={bytes.Length}");
    }

    private static string FindTemplatesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "templates");
            if (Directory.Exists(Path.Combine(candidate, "reports", ClinicalReportCatalog.HemodialysisRecord)))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException();
    }
}
