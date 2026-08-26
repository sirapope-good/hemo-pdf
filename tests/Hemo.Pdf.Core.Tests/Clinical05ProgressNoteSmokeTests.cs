using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
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

    [Fact]
    public void BudgetRowHeight_UsesTwoRowsPerPage()
    {
        var vm = MinimalViewModel(sessionCount: 0);
        var height = Clinical05ProgressNoteComposer.BudgetRowHeightMm(vm);
        Assert.True(height >= 90f);
        // Two empty slots share the page; one row must not claim the full content height.
        Assert.True(height < 200f);
    }

    [Fact]
    public void SoapChrome_OverridesColumnWidthsAndBandWeights()
    {
        var mixed = HprpChrome.ParseMixedColumns(["18mm", "3", "1", "1"]);
        Assert.Equal(4, mixed.Count);
        Assert.True(mixed[0].ConstantMm);
        Assert.Equal(18f, mixed[0].Value);
        Assert.False(mixed[1].ConstantMm);
        Assert.Equal(3f, mixed[1].Value);

        var bands = HprpChrome.ResolveBandWeights([1f, 3f, 1f, 1f], Clinical05SoapTableSection.DefaultSoapBandWeights);
        Assert.Equal([1f, 3f, 1f, 1f], bands);
    }

    [Fact]
    public void Compose_PutsThaiUrHeaderOnRepeatingPageSlot()
    {
        var composer = new Clinical05ProgressNoteComposer();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.ProgressNote,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical05ProgressNoteDataProvider.ReportTitle },
        };

        var layout = Assert.IsType<QuestLayout>(composer.Compose(MinimalViewModel(sessionCount: 1), context));
        Assert.NotNull(layout.Header);
        Assert.NotNull(layout.Content);
    }

    [Fact]
    public async Task Render_MultiPage_DoesNotCollapseToSinglePage()
    {
        var renderer = new Clinical05ProgressNoteReportRenderer(
            new Clinical05ProgressNoteDataProvider(),
            new Clinical05ProgressNoteComposer(),
            new QuestPdfRenderer());

        var json = System.Text.RegularExpressions.Regex.Replace(
            SampleJson,
            @"""sessions""\s*:\s*\[[\s\S]*?\]",
            $""" "sessions": [ {BuildSessionsJson(5)} ] """);

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.ProgressNote,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = Clinical05ProgressNoteDataProvider.ReportTitle },
            Data = JsonDocument.Parse(json).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        var pageCount = CountPdfPages(bytes);
        Assert.True(pageCount >= 2, $"Expected a multi-page report so the repeating header can apply, got {pageCount} page(s).");
    }

    private static Clinical05ProgressNoteReportViewModel MinimalViewModel(int sessionCount)
    {
        return new Clinical05ProgressNoteReportViewModel
        {
            Title = Clinical05ProgressNoteDataProvider.ReportTitle,
            MonthKey = "2026-08",
            Header = new Hemo.Pdf.Core.Models.Hemosheet.HemosheetReportViewModel
            {
                Patient = new Hemo.Pdf.Core.Models.Hemosheet.HemosheetPatientViewModel { Name = "Sample Patient" },
            },
            Sessions = Enumerable.Range(1, sessionCount)
                .Select(i => new Clinical05SoapSession
                {
                    HemodialysisId = Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{i:D12}").ToString(),
                    DateLabel = $"{i:00}/08/2026",
                    Subjective = "No complaint",
                })
                .ToList(),
        };
    }

    private static string BuildSessionsJson(int count) =>
        string.Join(
            ",",
            Enumerable.Range(1, count).Select(i => $$"""
                {
                  "hemodialysisId": "{{Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{i:D12}")}}",
                  "dateLabel": "{{i:00}}/08/2026",
                  "subjective": "No complaint session {{i}}",
                  "generalAppearance": "goodConscious",
                  "heent": "normal",
                  "lung": "normal",
                  "extremities": "normal",
                  "assessment": "ESRD c U/D DM",
                  "plan": "Continue HD",
                  "orderForOneDay": "CBC {{i}}",
                  "orderForContinuation": "EPO"
                }
                """));

    private static int CountPdfPages(byte[] pdf)
    {
        var text = System.Text.Encoding.ASCII.GetString(pdf);
        var tree = System.Text.RegularExpressions.Regex.Match(
            text,
            @"/Type\s*/Pages\b.*?/Count\s+(\d+)",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        if (tree.Success && int.TryParse(tree.Groups[1].Value, out var count) && count > 0)
            return count;

        return System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
    }
}
