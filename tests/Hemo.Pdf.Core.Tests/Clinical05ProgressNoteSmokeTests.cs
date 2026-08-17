using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical05ProgressNoteSmokeTests
{
    private const string SampleJson = """
        {
          "title": "Hemodialysis Progress note",
          "monthKey": "2026-08",
          "header": {
            "patient": {
              "name": "Sample Patient",
              "hn": "6512620",
              "identityNumber": "3101401131780",
              "age": 55,
              "allergies": ["ไม่มีแพ้ยา"],
              "coverage": "สปสช.",
              "diagnosis": "ESRD",
              "underlying": "DM"
            },
            "unit": { "id": -1, "fullName": "Hemodialysis Unit" },
            "layoutContext": {
              "reportSettings": { "showDateAndHdNo": true, "showHdPerWeek": false }
            }
          },
          "sessions": [
            {
              "hemodialysisId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "dateLabel": "12/08/2026",
              "subjective": "No complaint",
              "generalAppearance": "goodConscious",
              "heent": "normal",
              "lung": "normal",
              "extremities": "normal",
              "assessment": "ESRD c U/D DM",
              "plan": "Continue HD",
              "orderForOneDay": "CBC",
              "orderForContinuation": "EPO"
            }
          ]
        }
        """;

    static Clinical05ProgressNoteSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_DeserializesTrustedPayload()
    {
        var provider = new Clinical05ProgressNoteDataProvider();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.ProgressNote,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical05ProgressNoteDataProvider.ReportTitle },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var model = await provider.GetDataAsync(context, CancellationToken.None);
        var vm = Assert.IsType<Clinical05ProgressNoteReportViewModel>(model);

        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowDateAndHdNo);
        Assert.True(vm.Header.LayoutContext.ReportSettings.ShowHdPerWeek);
        Assert.Equal(Clinical05ProgressNoteDataProvider.ReportTitle, vm.Title);
        Assert.Equal("Sample Patient", vm.Header.Patient.Name);
        var session = Assert.Single(vm.Sessions);
        Assert.Equal("12/08/2026", session.DateLabel);
        Assert.Equal("goodConscious", session.GeneralAppearance);
        Assert.Equal("CBC", session.OrderForOneDay);
    }

    [Fact]
    public async Task Render_ProducesPdfBytes()
    {
        var renderer = new Clinical05ProgressNoteReportRenderer(
            new Clinical05ProgressNoteDataProvider(),
            new Clinical05ProgressNoteComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.ProgressNote,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical05ProgressNoteDataProvider.ReportTitle },
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
