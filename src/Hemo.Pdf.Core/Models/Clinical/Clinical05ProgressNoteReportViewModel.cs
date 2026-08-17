using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-05 Hemodialysis Progress note (SOAP per session).</summary>
public sealed class Clinical05ProgressNoteReportViewModel
{
    public string Title { get; init; } = "Hemodialysis Progress note";

    public string MonthKey { get; init; } = string.Empty;

    public HemosheetReportViewModel Header { get; init; } = new();

    public IReadOnlyList<Clinical05SoapSession> Sessions { get; init; } = [];
}

public sealed class Clinical05SoapSession
{
    public string HemodialysisId { get; init; } = string.Empty;

    public string DateLabel { get; init; } = string.Empty;

    public string? Subjective { get; init; }

    public string? GeneralAppearance { get; init; }

    public string? GeneralAppearanceOther { get; init; }

    public string? Heent { get; init; }

    public string? HeentNote { get; init; }

    public string? Lung { get; init; }

    public string? LungNote { get; init; }

    public string? Extremities { get; init; }

    public string? ExtremitiesNote { get; init; }

    public string? ObjectiveOther { get; init; }

    public string? Assessment { get; init; }

    public string? Plan { get; init; }

    public string? OrderForOneDay { get; init; }

    public string? OrderForContinuation { get; init; }
}
