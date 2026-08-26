using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical01HctEpoSmokeTests
{
    private const string SampleJson = """
        {
          "title": "Hemodialysis Review Hct and EPO",
          "year": 2023,
          "header": {
            "logoBase64": null,
            "patient": {
              "name": "Sample Patient",
              "hn": "6512620",
              "identityNumber": "3101401131780",
              "age": 55,
              "allergies": ["ไม่มีแพ้ยา"],
              "coverage": "สปสช.",
              "diagnosis": "ESRD",
              "underlying": "ESRD"
            },
            "unit": { "id": -1, "fullName": "Hemodialysis Unit" },
            "layoutContext": {
              "reportSettings": { "showDateAndHdNo": false, "showHdPerWeek": true }
            }
          },
          "months": [
            {
              "monthIndex": 2,
              "monthLabel": "ก.พ.",
              "entries": [
                {
                  "dayLabel": "06-02-2023",
                  "hb": "9.5",
                  "hct": "28.0",
                  "labIsHistorical": true,
                  "epoName": "Nesp 40 mcg",
                  "frequencyText": "1 dose within 1 week",
                  "injectionDate": "10/02/2023",
                  "remarks": null
                },
                {
                  "dayLabel": "20-02-2023",
                  "hb": "9.8",
                  "hct": "29.4",
                  "labIsHistorical": false,
                  "epoName": "Eprex",
                  "frequencyText": "2 dose within 1 week",
                  "injectionDate": "01/02/2023",
                  "remarks": null
                }
              ]
            }
          ],
          "coPayCriteria": {
            "title": "ปริมาณยาที่มีสิทธิได้รับโดยไม่ต้องร่วมจ่าย",
            "nhsoRules": [
              { "condition": "Hb < 10", "injectionsPerWeek": "2" },
              { "condition": "Hb 10-11.9", "injectionsPerWeek": "1" },
              { "condition": "Hb ≥ 12", "injectionsPerWeek": "0" }
            ],
            "ssoRules": [
              { "medicine": "Espogen 4000 U", "hctLe36": "3", "hctGt36": "2", "hctGe39": "0" },
              { "medicine": "Hemax 4000 U", "hctLe36": "2", "hctGt36": "1", "hctGe39": "0" }
            ]
          }
        }
        """;

    static Clinical01HctEpoSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_DeserializesTrustedPayload()
    {
        var provider = new Clinical01HctEpoDataProvider();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HctEpo,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical01HctEpoDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var model = await provider.GetDataAsync(context, CancellationToken.None);
        var vm = Assert.IsType<Hemo.Pdf.Core.Models.Clinical.HctEpoReportViewModel>(model);

        Assert.Equal(12, vm.Months.Count);
        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowDateAndHdNo);
        Assert.True(vm.Header.LayoutContext.ReportSettings.ShowHdPerWeek);
        Assert.Equal(Clinical01HctEpoDataProvider.ReportTitle, vm.Title);
        Assert.Equal("Sample Patient", vm.Header.Patient.Name);
        var feb = Assert.Single(vm.Months, m => m.MonthIndex == 2);
        Assert.Equal(2, feb.Entries.Count);
        Assert.Equal("06-02-2023", feb.Entries[0].DayLabel);
        Assert.True(feb.Entries[0].LabIsHistorical);
        Assert.Equal("Nesp 40 mcg", feb.Entries[0].EpoName);
        Assert.Equal("20-02-2023", feb.Entries[1].DayLabel);
        Assert.False(feb.Entries[1].LabIsHistorical);
        Assert.NotEmpty(vm.CoPayCriteria.NhsoRules);
    }

    [Fact]
    public async Task Render_ProducesPdfBytes()
    {
        var renderer = new Clinical01HctEpoReportRenderer(
            new Clinical01HctEpoDataProvider(),
            new Clinical01HctEpoComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HctEpo,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical01HctEpoDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task Render_SwappedHbHctColumnPlan_ProducesPdfBytes()
    {
        var defaults = HprpWidgetRecipes.ClinicalHctEpoAnnualTable.DefaultColumnPlan;
        var swapped = new List<HprpColumnPlanItem>
        {
            new() { Bind = "hct", LabelKey = "colHct" },
            new() { Bind = "hb", LabelKey = "colHb" },
        };
        swapped.AddRange(defaults.Skip(2));

        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.HctEpo,
                DisplayName = Clinical01HctEpoDataProvider.ReportTitle,
                DataAdapter = HprpDataAdapterIds.Clinical01HctEpo,
            },
            Layout = new HprpLayout
            {
                Header = new HprpLayoutNode { Widget = HprpWidgetIds.ThaiUrHeader },
                Body =
                [
                    new HprpLayoutNode
                    {
                        Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable,
                        ColumnPlan = swapped,
                    },
                    new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoCopay },
                ],
            },
        };

        var renderer = new Clinical01HctEpoReportRenderer(
            new Clinical01HctEpoDataProvider(),
            new Clinical01HctEpoComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HctEpo,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical01HctEpoDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
            LayoutPackage = package,
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public async Task Render_WithHprpPackage_ProducesPdfBytes()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var store = new Hemo.Pdf.Application.Hprp.FileHprpTemplateStore(
            Microsoft.Extensions.Options.Options.Create(
                new Hemo.Pdf.Application.Hprp.HprpTemplateOptions
                {
                    RootPath = templatesRoot,
                    PackagesRootPath = Path.Combine(templatesRoot, "_no-packages"),
                }));

        var renderer = new Clinical01HctEpoReportRenderer(
            new Clinical01HctEpoDataProvider(),
            new Clinical01HctEpoComposer(store),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HctEpo,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical01HctEpoDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task Render_WithPackedHprp_ProducesPdfBytes()
    {
        var packages = Path.Combine(Path.GetTempPath(), "hprp-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packages);
        var options = Microsoft.Extensions.Options.Options.Create(
            new Hemo.Pdf.Application.Hprp.HprpTemplateOptions
            {
                RootPath = HprpTestAssets.TemplatesRoot(),
                PackagesRootPath = packages,
                PackagesWritePath = packages,
            });
        var store = new Hemo.Pdf.Application.Hprp.FileHprpTemplateStore(options);
        var pack = new Hemo.Pdf.Application.Hprp.HprpPackService(options, store);
        await pack.PackTemplateIdAsync(ClinicalReportCatalog.HctEpo);
        store.Invalidate();

        var renderer = new Clinical01HctEpoReportRenderer(
            new Clinical01HctEpoDataProvider(),
            new Clinical01HctEpoComposer(store),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.HctEpo,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical01HctEpoDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.NotNull(store.TryGetCached("local", ClinicalReportCatalog.HctEpo));
    }

    [Fact]
    public void BudgetMonthRowHeight_IsTallEnoughForThreeEntries()
    {
        var rowH = Clinical01HctEpoComposer.BudgetMonthRowHeightMm(
            Hemo.Pdf.Core.Models.Clinical.HctEpoCoPayCriteria.CreateDefault());

        Assert.True(rowH >= 12f, $"expected month row ≥ 12mm, got {rowH}");
        Assert.True(rowH < 30f, $"expected month row < 30mm, got {rowH}");
    }
}
