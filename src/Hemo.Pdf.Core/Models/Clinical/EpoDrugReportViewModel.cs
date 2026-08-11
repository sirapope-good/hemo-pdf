using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-02 Erythropoietin Drug Record (patient + month + medicine).</summary>
public sealed class EpoDrugReportViewModel
{
    public string Title { get; init; } = "Erythropoietin Drug Record";

    public HemosheetReportViewModel Header { get; init; } = new();

    public EpoDrugMeta Meta { get; init; } = new();

    public IReadOnlyList<EpoDrugInjectionRow> Rows { get; init; } = [];

    public HctEpoCoPayCriteria CoPayCriteria { get; init; } = HctEpoCoPayCriteria.CreateDefault();
}

public sealed class EpoDrugMeta
{
    public string MonthKey { get; init; } = string.Empty;
    public string MonthLabel { get; init; } = string.Empty;
    public int YearBe { get; init; }
    public int MedicineId { get; init; }
    public string EpoName { get; init; } = string.Empty;
    public string NeedlesPerWeek { get; init; } = string.Empty;
}

public sealed class EpoDrugInjectionRow
{
    public string DateLabel { get; init; } = string.Empty;
    public int DoseIndex { get; init; }
    public string? StickerText { get; init; }
    public string NurseName { get; init; } = string.Empty;
    public string? Remarks { get; init; }
}
