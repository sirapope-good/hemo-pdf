using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Deserializes trusted clinical-05-checklist report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical05ProgressNoteChecklistDataProvider : IReportDataProvider
{
    private const string DefaultReportCode = "DOC-PROG-NOTE-RP-001";

    public static string ReportTitle => ClinicalReportCatalog.TryGetDefinition(
        ClinicalReportCatalog.ProgressNoteChecklist,
        out var def)
        ? def!.DisplayName
        : "Doctor progress note report";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Data is not JsonElement json || json.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"{ClinicalReportCatalog.ProgressNoteChecklist} requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<Clinical05ChecklistWireModel>(json.GetRawText(), JsonOptions)
            ?? new Clinical05ChecklistWireModel();

        var result = new Clinical05ProgressNoteChecklistReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            ReportCode = string.IsNullOrWhiteSpace(wire.ReportCodeValue) ? DefaultReportCode : wire.ReportCodeValue!,
            FromYearMonth = wire.FromYearMonth ?? string.Empty,
            ToYearMonth = wire.ToYearMonth ?? string.Empty,
            RangeLabel = wire.RangeLabel ?? string.Empty,
            Patient = new Clinical05ProgressNoteChecklistPatient
            {
                Name = wire.Patient?.Name ?? "—",
                HospitalNumber = wire.Patient?.HospitalNumber ?? "—",
                BirthDateLabel = wire.Patient?.BirthDateLabel ?? "—",
                SessionsPerWeekLabel = wire.Patient?.SessionsPerWeekLabel ?? "—",
                DialysisDays = wire.Patient?.DialysisDays ?? "—",
                CoverageScheme = wire.Patient?.CoverageScheme ?? "—",
                DialysisMode = wire.Patient?.DialysisMode ?? "—",
                Underlying = wire.Patient?.Underlying ?? "—",
            },
            Columns = (wire.Columns ?? [])
                .Select(c => new Clinical05ProgressNoteChecklistColumn
                {
                    YearMonth = c.YearMonth ?? string.Empty,
                    CalendarYear = c.CalendarYear,
                    CalendarMonth = c.CalendarMonth,
                })
                .ToList(),
            YearSpans = (wire.YearSpans ?? [])
                .Select(s => new Clinical05ProgressNoteChecklistYearSpan
                {
                    Year = s.Year,
                    ColSpan = s.ColSpan,
                })
                .ToList(),
            ChecklistItems = (wire.ChecklistItems ?? [])
                .Select(i => new Clinical05ProgressNoteChecklistItem
                {
                    Label = i.Label ?? string.Empty,
                    Group = i.Group,
                    Marks = i.Marks ?? [],
                })
                .ToList(),
            TextNotes = (wire.TextNotes ?? [])
                .Select(n => new Clinical05ProgressNoteChecklistTextNote
                {
                    YearMonth = n.YearMonth ?? string.Empty,
                    MonthLabel = n.MonthLabel ?? string.Empty,
                    Content = n.Content ?? string.Empty,
                })
                .ToList(),
        };

        return Task.FromResult<object>(result);
    }

    private sealed class Clinical05ChecklistWireModel
    {
        public string? Title { get; set; }
        public string? ReportCodeValue { get; set; }
        public string? FromYearMonth { get; set; }
        public string? ToYearMonth { get; set; }
        public string? RangeLabel { get; set; }
        public Clinical05ChecklistPatientWire? Patient { get; set; }
        public List<Clinical05ChecklistColumnWire>? Columns { get; set; }
        public List<Clinical05ChecklistYearSpanWire>? YearSpans { get; set; }
        public List<Clinical05ChecklistItemWire>? ChecklistItems { get; set; }
        public List<Clinical05ChecklistTextNoteWire>? TextNotes { get; set; }
    }

    private sealed class Clinical05ChecklistPatientWire
    {
        public string? Name { get; set; }
        public string? HospitalNumber { get; set; }
        public string? BirthDateLabel { get; set; }
        public string? SessionsPerWeekLabel { get; set; }
        public string? DialysisDays { get; set; }
        public string? CoverageScheme { get; set; }
        public string? DialysisMode { get; set; }
        public string? Underlying { get; set; }
    }

    private sealed class Clinical05ChecklistColumnWire
    {
        public string? YearMonth { get; set; }
        public int CalendarYear { get; set; }
        public int CalendarMonth { get; set; }
    }

    private sealed class Clinical05ChecklistYearSpanWire
    {
        public int Year { get; set; }
        public int ColSpan { get; set; }
    }

    private sealed class Clinical05ChecklistItemWire
    {
        public string? Label { get; set; }
        public string? Group { get; set; }
        public List<string>? Marks { get; set; }
    }

    private sealed class Clinical05ChecklistTextNoteWire
    {
        public string? YearMonth { get; set; }
        public string? MonthLabel { get; set; }
        public string? Content { get; set; }
    }
}
