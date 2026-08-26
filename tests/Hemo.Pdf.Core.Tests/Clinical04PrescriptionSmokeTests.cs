using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical04_Prescription;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical04PrescriptionSmokeTests
{
    private const string SampleJson = """
        {
          "title": "Hemodialysis Prescription",
          "reportDate": "26/08/2026",
          "orderDate": "26/08/2026",
          "orderSubtitle": "Hemodialysis treatment order dated 26/08/2026",
          "header": {
            "patient": {
              "name": "Sample Patient",
              "hn": "6512620",
              "identityNumber": "3101401131780",
              "age": 55,
              "allergies": ["ไม่มีแพ้ยา"],
              "coverage": "สปสช.",
              "diagnosis": "ESRD",
              "underlying": "DM",
              "hdPerWeek": "3"
            },
            "unit": { "id": -1, "fullName": "Hemodialysis Unit" },
            "layoutContext": {
              "reportSettings": { "showDateAndHdNo": true, "showHdPerWeek": false }
            }
          },
          "dialysisFields": [
            { "label": "Hemodialysis:", "value": "3 time/week" },
            { "label": "Dialysis Hours:", "value": "4H 0M" },
            { "label": "Dry weight:", "value": "62.5 kg" }
          ],
          "medicinePrescriptionLines": [
            "Eprex (IU) : 4000 IU x 1"
          ],
          "medHistoryLines": [
            "ASA (mg) : 1 x OD (PC)"
          ],
          "isSigned": true,
          "doctorName": "Dr. Sample",
          "doctorUpdated": "26/08/2026 14:30"
        }
        """;

    static Clinical04PrescriptionSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_DeserializesTrustedPayload()
    {
        var provider = new Clinical04PrescriptionDataProvider();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Prescription,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical04PrescriptionDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var model = await provider.GetDataAsync(context, CancellationToken.None);
        var vm = Assert.IsType<Clinical04PrescriptionReportViewModel>(model);

        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowDateAndHdNo);
        Assert.True(vm.Header.LayoutContext.ReportSettings.ShowHdPerWeek);
        Assert.Equal(Clinical04PrescriptionDataProvider.ReportTitle, vm.Title);
        Assert.Equal("Sample Patient", vm.Header.Patient.Name);
        Assert.Equal(3, vm.DialysisFields.Count);
        Assert.Single(vm.MedicinePrescriptionLines);
        Assert.Single(vm.MedHistoryLines);
        Assert.True(vm.IsSigned);
        Assert.Equal("Dr. Sample", vm.DoctorName);
    }

    [Fact]
    public async Task Render_ProducesPdfBytes()
    {
        var renderer = new Clinical04PrescriptionReportRenderer(
            new Clinical04PrescriptionDataProvider(),
            new Clinical04PrescriptionComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Prescription,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical04PrescriptionDataProvider.ReportTitle },
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
    public async Task Render_BlankSparseData_StillProducesPdf()
    {
        const string blankJson = """
            {
              "title": "Hemodialysis Prescription",
              "header": {
                "patient": { "name": "Blank Patient", "hn": "1" },
                "unit": { "id": -1, "fullName": "Unit" },
                "layoutContext": { "reportSettings": { "showHdPerWeek": true } }
              },
              "dialysisFields": [],
              "medicinePrescriptionLines": [],
              "medHistoryLines": [],
              "isSigned": false
            }
            """;

        var renderer = new Clinical04PrescriptionReportRenderer(
            new Clinical04PrescriptionDataProvider(),
            new Clinical04PrescriptionComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Prescription,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical04PrescriptionDataProvider.ReportTitle },
            Data = JsonDocument.Parse(blankJson).RootElement.Clone(),
            LayoutPackage = new HprpPackage
            {
                Manifest = new HprpManifest
                {
                    Id = ClinicalReportCatalog.Prescription,
                    DisplayName = "x",
                    DataAdapter = HprpDataAdapterIds.Clinical04Prescription,
                },
                Layout = new HprpLayout
                {
                    Header = new HprpLayoutNode { Widget = HprpWidgetIds.ThaiUrHeader },
                    Body =
                    [
                        new HprpLayoutNode
                        {
                            Widget = HprpWidgetIds.ClinicalPrescriptionColumns,
                            Chrome = new HprpChrome { Border = "thin", FontSize = 7.5f },
                        },
                    ],
                },
            },
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public void BudgetBlockHeight_FillsMostOfPage()
    {
        var height = Clinical04PrescriptionComposer.BudgetBlockHeightMm();
        Assert.True(height >= 120f);
        Assert.True(height < 280f);
    }
}
