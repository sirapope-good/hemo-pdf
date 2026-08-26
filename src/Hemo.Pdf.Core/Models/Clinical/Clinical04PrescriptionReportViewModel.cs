using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-04 Hemodialysis Prescription (two equal columns).</summary>
public sealed class Clinical04PrescriptionReportViewModel
{
    public string Title { get; init; } = "Hemodialysis Prescription";

    public string ReportDate { get; init; } = string.Empty;

    public string OrderDate { get; init; } = string.Empty;

    public string OrderSubtitle { get; init; } = string.Empty;

    public HemosheetReportViewModel Header { get; init; } = new();

    public IReadOnlyList<LabelValue> DialysisFields { get; init; } = [];

    public IReadOnlyList<string> MedicinePrescriptionLines { get; init; } = [];

    public IReadOnlyList<string> MedHistoryLines { get; init; } = [];

    public bool IsSigned { get; init; }

    public string DoctorName { get; init; } = string.Empty;

    public string DoctorUpdated { get; init; } = string.Empty;
}
