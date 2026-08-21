using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.ThaiUr;

namespace Hemo.Pdf.Sections.Hemosheet;

/// <summary>
/// Shared dialysis-grid column rules for dense Default/ThaiUR forms.
/// HDF (online) adds CICM-style Substitute total/rate between VP and TMP —
/// same data as Telerik <c>SAV</c>/<c>SRate</c> when <c>DialysisPrescription.Mode == HDF</c>.
/// </summary>
public static class HemosheetDialysisColumns
{
    /// <summary>Base CICM/ThaiUR monitoring columns (no Substitute).</summary>
    public static readonly (string Head, string Unit)[] BaseColumnDefs =
    [
        ("Time", ""), ("BP", "mmHg"), ("MAP", "mmHg"), ("Pulse", "/min"),
        ("EBFR", "ml/min"), ("AP", "mmHg"), ("VP", "mmHg"), ("TMP", "mmHg"),
        ("Cond.", "mS/cm"), ("UFR", "ml/hr"), ("Total UF", "ml"),
    ];

    /// <summary>Index in <see cref="BaseColumnDefs"/> after which Substitute columns are inserted (after VP).</summary>
    public const int SubstituteInsertAfterIndex = 6; // VP

    public static bool ShowHdf(HemosheetReportViewModel vm)
    {
        if (vm.LayoutContext.Features.TryGetValue("showHdfColumns", out var flagged) && flagged)
            return true;

        if (string.Equals(vm.LayoutContext.DialysisMode, "HDF", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(vm.DialysisPrescription.Mode, "HDF", StringComparison.OrdinalIgnoreCase);
    }

    public static int DataColumnCount(bool showHdf) =>
        BaseColumnDefs.Length + (showHdf ? 2 : 0);

    /// <summary>Data columns plus the trailing Note column.</summary>
    public static int HeaderColumnCount(bool showHdf) =>
        DataColumnCount(showHdf) + 1;

    public static float DataColumnWidthMm(bool showHdf) =>
        showHdf ? 9.5f : 11.2f;

    /// <summary>Single-row header (HD) or dual-row parent+sub (HDF).</summary>
    public static float HeaderHeightMm(bool showHdf, float rowHeightMm) =>
        showHdf ? rowHeightMm + 6.2f : rowHeightMm + 3.2f;

    /// <summary>
    /// Fluid-summary box spans must equal data columns + Note.
    /// HDF adds two Substitute columns — Extra-fluid absorbs the extra width.
    /// </summary>
    public static (int Span, string Label, string Value)[] FluidBoxes(
        HemosheetReportViewModel vm,
        bool showHdf)
    {
        var extraSpan = showHdf ? 5 : 3;
        return
        [
            (2, "NSS", ThaiUrData.Ml(ThaiUrData.NssMl(vm))),
            (2, "50% Glucose", "-"),
            (extraSpan, "Extra-fluid", ThaiUrData.Ml(ThaiUrData.ExtraFluidMl(vm))),
            (3, "Total fluid replacment", "-"),
            (1, "Total UF", ThaiUrData.Ml(ThaiUrData.TotalUfMl(vm))),
            (1, "Net fluid balance", ThaiUrData.Ml(ThaiUrData.NetFluidBalanceMl(vm))),
        ];
    }

    /// <summary>
    /// Cell values in column order (excludes Note). Blank placeholders when <paramref name="rec"/> is null.
    /// </summary>
    public static string[] CellValues(HemosheetDialysisRecordViewModel? rec, bool showHdf)
    {
        var count = DataColumnCount(showHdf);
        if (rec is null)
            return new string[count];

        var list = new List<string>(count)
        {
            ThaiUrData.Time(rec.Timestamp),
            ThaiUrData.Bp(rec.Bps, rec.Bpd),
            ThaiUrData.Map(rec.Bps, rec.Bpd) ?? "",
            ThaiUrData.Num(rec.Hr),
            ThaiUrData.Num(rec.Bfr),
            ThaiUrData.Num(rec.Ap),
            ThaiUrData.Num(rec.Vp),
        };

        if (showHdf)
        {
            list.Add(BlankDash(ThaiUrData.Num(rec.HdfVolume)));
            list.Add(BlankDash(ThaiUrData.Num(rec.HdfRate)));
        }

        list.Add(ThaiUrData.Num(rec.Tmp));
        list.Add(ThaiUrData.Num(rec.Dc));
        list.Add(rec.UfRate is not null ? ThaiUrData.Num(rec.UfRate * 1000) : "");
        list.Add(rec.UfTotal is not null ? ThaiUrData.Num(rec.UfTotal * 1000) : "");

        // Match existing dialysis cells: "-" paints as empty.
        for (var i = 0; i < list.Count; i++)
            list[i] = BlankDash(list[i]);

        return list.ToArray();
    }

    private static string BlankDash(string value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? "" : value;
}
