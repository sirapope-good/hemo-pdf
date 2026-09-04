namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-05 monthly checklist (Hemodialysis Progress note, landscape).</summary>
public sealed class Clinical05ProgressNoteChecklistReportViewModel
{
    public string Title { get; init; } = "Hemodialysis Progress note";

    public string ReportCode { get; init; } = "DOC-PROG-NOTE-RP-001";

    public string FromYearMonth { get; init; } = string.Empty;

    public string ToYearMonth { get; init; } = string.Empty;

    public string RangeLabel { get; init; } = string.Empty;

    public Clinical05ProgressNoteChecklistPatient Patient { get; init; } = new();

    public IReadOnlyList<Clinical05ProgressNoteChecklistColumn> Columns { get; init; } =
        Array.Empty<Clinical05ProgressNoteChecklistColumn>();

    public IReadOnlyList<Clinical05ProgressNoteChecklistYearSpan> YearSpans { get; init; } =
        Array.Empty<Clinical05ProgressNoteChecklistYearSpan>();

    public IReadOnlyList<Clinical05ProgressNoteChecklistItem> ChecklistItems { get; init; } =
        Array.Empty<Clinical05ProgressNoteChecklistItem>();

    public IReadOnlyList<Clinical05ProgressNoteChecklistTextNote> TextNotes { get; init; } =
        Array.Empty<Clinical05ProgressNoteChecklistTextNote>();
}

public sealed class Clinical05ProgressNoteChecklistPatient
{
    public string Name { get; init; } = "—";

    public string HospitalNumber { get; init; } = "—";

    public string BirthDateLabel { get; init; } = "—";

    public string SessionsPerWeekLabel { get; init; } = "—";

    public string DialysisDays { get; init; } = "—";

    public string CoverageScheme { get; init; } = "—";

    public string DialysisMode { get; init; } = "—";

    public string Underlying { get; init; } = "—";
}

public sealed class Clinical05ProgressNoteChecklistColumn
{
    public string YearMonth { get; init; } = string.Empty;

    public int CalendarYear { get; init; }

    public int CalendarMonth { get; init; }
}

public sealed class Clinical05ProgressNoteChecklistYearSpan
{
    public int Year { get; init; }

    public int ColSpan { get; init; }
}

public sealed class Clinical05ProgressNoteChecklistItem
{
    public string Label { get; init; } = string.Empty;

    public string? Group { get; init; }

    public IReadOnlyList<string> Marks { get; init; } = Array.Empty<string>();
}

public sealed class Clinical05ProgressNoteChecklistTextNote
{
    public string YearMonth { get; init; } = string.Empty;

    public string MonthLabel { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}
