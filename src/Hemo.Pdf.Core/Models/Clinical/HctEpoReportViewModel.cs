using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-01 Hemodialysis Review Hct and EPO (annual).</summary>
public sealed class HctEpoReportViewModel
{
    public string Title { get; init; } = "Hemodialysis Review Hct & EPO";

    public int Year { get; init; }

    /// <summary>
    /// Minimal hemosheet VM used only by <c>ThaiUrReportHeader</c>
    /// (<c>ShowDateAndHdNo</c> false; <c>ShowHdPerWeek</c> true for HD … T/Wk).
    /// </summary>
    public HemosheetReportViewModel Header { get; init; } = new();

    /// <summary>Always 12 rows (Jan–Dec), empty cells allowed.</summary>
    public IReadOnlyList<HctEpoMonthRow> Months { get; init; } = [];

    /// <summary>
    /// Co-pay eligibility reference tables.
    /// TODO(tenant): allow tenant override / replace of co-pay criteria block.
    /// </summary>
    public HctEpoCoPayCriteria CoPayCriteria { get; init; } = HctEpoCoPayCriteria.CreateDefault();
}

public sealed class HctEpoMonthRow
{
    /// <summary>1 = January … 12 = December.</summary>
    public int MonthIndex { get; init; }

    /// <summary>Thai short month label (ม.ค. … ธ.ค.).</summary>
    public string MonthLabel { get; init; } = string.Empty;

    /// <summary>
    /// Ruled sub-rows for this month (labs + ESA). Empty slots are still drawn
    /// so each month keeps <see cref="HctEpoMonthLabels.SlotsPerMonth"/> lines.
    /// </summary>
    public IReadOnlyList<HctEpoMonthEntry> Entries { get; init; } = [];
}

/// <summary>
/// One ruled sub-row inside a month block (aligned day + optional lab + optional ESA).
/// </summary>
public sealed class HctEpoMonthEntry
{
    /// <summary>Day-of-month label (e.g. <c>01</c>) aligned with lab / injection.</summary>
    public string? DayLabel { get; init; }

    public string? Hb { get; init; }
    public string? Hct { get; init; }

    /// <summary>
    /// When true, Hb/Hct render in muted gray (earlier reading in the same month).
    /// Latest lab in the month stays normal black.
    /// </summary>
    public bool LabIsHistorical { get; init; }

    public string? EpoName { get; init; }
    public string? FrequencyText { get; init; }
    public string? InjectionDate { get; init; }
    public string? Remarks { get; init; }
}

public sealed class HctEpoCoPayCriteria
{
    public string Title { get; init; } = "ปริมาณยาที่มีสิทธิได้รับโดยไม่ต้องร่วมจ่าย";

    public IReadOnlyList<HctEpoNhsoRuleRow> NhsoRules { get; init; } = [];

    public IReadOnlyList<HctEpoSsoRuleRow> SsoRules { get; init; } = [];

    public static HctEpoCoPayCriteria CreateDefault() => new()
    {
        Title = "ปริมาณยาที่มีสิทธิได้รับโดยไม่ต้องร่วมจ่าย",
        NhsoRules =
        [
            new() { Condition = "Hb < 10", InjectionsPerWeek = "2" },
            new() { Condition = "Hb 10-11.9", InjectionsPerWeek = "1" },
            new() { Condition = "Hb ≥ 12", InjectionsPerWeek = "0" },
        ],
        SsoRules =
        [
            new()
            {
                Medicine = "Espogen 4000 U",
                HctLe36 = "3",
                HctGt36 = "2",
                HctGe39 = "0",
            },
            new()
            {
                Medicine = "Hemax 4000 U",
                HctLe36 = "2",
                HctGt36 = "1",
                HctGe39 = "0",
            },
        ],
    };
}

public sealed class HctEpoNhsoRuleRow
{
    public string Condition { get; init; } = string.Empty;
    public string InjectionsPerWeek { get; init; } = string.Empty;
}

public sealed class HctEpoSsoRuleRow
{
    public string Medicine { get; init; } = string.Empty;
    public string HctLe36 { get; init; } = string.Empty;
    public string HctGt36 { get; init; } = string.Empty;
    public string HctGe39 { get; init; } = string.Empty;
}

/// <summary>Thai month abbreviations for the annual Hct/EPO table.</summary>
public static class HctEpoMonthLabels
{
    /// <summary>Paper form rules ~2 lines; we keep 3 empty slots per month.</summary>
    public const int SlotsPerMonth = 3;

    public static readonly string[] ThaiShort =
    [
        "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
        "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค.",
    ];

    public static IReadOnlyList<HctEpoMonthRow> EmptyYear() =>
        Enumerable.Range(1, 12)
            .Select(m => new HctEpoMonthRow
            {
                MonthIndex = m,
                MonthLabel = ThaiShort[m - 1],
            })
            .ToList();
}
