using System.Globalization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// One physical column in a matrix checklist grid (item label or one month).
/// </summary>
public readonly struct HprpMatrixColumnSpec
{
    public bool ConstantMm { get; init; }
    public float Value { get; init; }
}

/// <summary>
/// Expands matrix <c>chrome.columnWidths</c> (2 zones: item + month-band) into
/// 1 + N physical columns. Shared by QuestPDF and Studio HTML.
/// </summary>
public static class HprpMatrixColumnPlan
{
    public const float DefaultLabelMm = 46f;

    /// <summary>
    /// Default tokens when omitted/invalid: fixed item column + flexible month band.
    /// </summary>
    public static readonly string[] DefaultTokens = ["46mm", "*"];

    /// <summary>
    /// Resolves <paramref name="columnWidths"/> into item + <paramref name="monthCount"/> month columns.
    /// Tokens: index 0 = item zone, index 1 = month-band (split equally across months).
    /// Each token is <c>Nmm</c> (constant) or relative weight (<c>*</c> / number).
    /// </summary>
    public static IReadOnlyList<HprpMatrixColumnSpec> Resolve(
        IReadOnlyList<string>? columnWidths,
        int monthCount)
    {
        var months = Math.Max(0, monthCount);
        var zones = HprpChrome.ParseMixedColumns(columnWidths);
        if (zones.Count < 2)
            zones = HprpChrome.ParseMixedColumns(DefaultTokens);

        var item = zones[0];
        var band = zones[1];

        var result = new List<HprpMatrixColumnSpec>(1 + Math.Max(1, months))
        {
            new() { ConstantMm = item.ConstantMm, Value = item.Value },
        };

        if (months == 0)
            return result;

        if (band.ConstantMm)
        {
            var perMonth = band.Value / months;
            if (perMonth <= 0)
                perMonth = 0.1f;
            for (var i = 0; i < months; i++)
                result.Add(new HprpMatrixColumnSpec { ConstantMm = true, Value = perMonth });
        }
        else
        {
            // Equal share of the band weight across months.
            var perMonth = band.Value / months;
            if (perMonth <= 0)
                perMonth = 0.1f;
            for (var i = 0; i < months; i++)
                result.Add(new HprpMatrixColumnSpec { ConstantMm = false, Value = perMonth });
        }

        return result;
    }

    /// <summary>
    /// Formats a zone token for inspector / chrome persistence.
    /// </summary>
    public static string FormatToken(bool constantMm, float value) =>
        constantMm
            ? string.Create(CultureInfo.InvariantCulture, $"{value:0.##}mm")
            : value is >= 0.999f and <= 1.001f
                ? "*"
                : value.ToString("0.##", CultureInfo.InvariantCulture);
}
