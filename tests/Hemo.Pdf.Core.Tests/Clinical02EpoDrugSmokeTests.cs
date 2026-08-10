using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical02EpoDrugSmokeTests
{
    private const string SampleJson = """
        {
          "title": "Erythropoietin Drug Record",
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
              "underlying": "ESRD",
              "hdPerWeek": "3"
            },
            "unit": { "id": -1, "fullName": "Hemodialysis Unit" },
            "layoutContext": {
              "reportSettings": { "showDateAndHdNo": false, "showHdPerWeek": true }
            }
          },
          "meta": {
            "monthKey": "2023-02",
            "monthLabel": "ก.พ.",
            "yearBe": 2566,
            "medicineId": 12,
            "epoName": "Eprex 4000",
            "needlesPerWeek": "2"
          },
          "rows": [
            {
              "dateLabel": "06/02/2023",
              "doseIndex": 1,
              "stickerText": null,
              "nurseName": "Nurse A",
              "remarks": null
            },
            {
              "dateLabel": "13/02/2023",
              "doseIndex": 2,
              "stickerText": null,
              "nurseName": "Nurse B",
              "remarks": "note"
            }
          ],
          "coPayCriteria": {
            "title": "ปริมาณยาที่มีสิทธิได้รับโดยไม่ต้องร่วมจ่าย",
            "nhsoRules": [
              { "condition": "Hb < 10", "injectionsPerWeek": "2" }
            ],
            "ssoRules": [
              { "medicine": "Espogen 4000 U", "hctLe36": "3", "hctGt36": "2", "hctGe39": "0" }
            ]
          }
        }
        """;

    static Clinical02EpoDrugSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_DeserializesTrustedPayload()
    {
        var provider = new Clinical02EpoDrugDataProvider();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.EpoDrug,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical02EpoDrugDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var model = await provider.GetDataAsync(context, CancellationToken.None);
        var vm = Assert.IsType<Hemo.Pdf.Core.Models.Clinical.EpoDrugReportViewModel>(model);

        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowDateAndHdNo);
        Assert.True(vm.Header.LayoutContext.ReportSettings.ShowHdPerWeek);
        Assert.Equal("Eprex 4000", vm.Meta.EpoName);
        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal(1, vm.Rows[0].DoseIndex);
        Assert.Null(vm.Rows[0].StickerText);
        Assert.NotEmpty(vm.CoPayCriteria.NhsoRules);
    }

    [Fact]
    public async Task Render_ProducesPdfBytes()
    {
        var renderer = new Clinical02EpoDrugReportRenderer(
            new Clinical02EpoDrugDataProvider(),
            new Clinical02EpoDrugComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.EpoDrug,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical02EpoDrugDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
