namespace Hemo.Pdf.Sections.Content;

public static class DataGridRows
{
    /// <summary>
    /// Frequency section titles from clinical-07 lab matrix (backend <c>Clinical07LabMatrix.SectionTitle</c>).
    /// Only these rows merge as section bands — not every row with an empty DATE tail.
    /// </summary>
    private static readonly HashSet<string> SectionBandTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "1 Month",
        "3 Month",
        "6 Month",
        "1 Year",
        "Other",
    };

    public static bool IsSectionBand(IReadOnlyList<string?> row)
    {
        if (row is null || row.Count < 2 || string.IsNullOrWhiteSpace(row[0]))
            return false;

        var label = row[0]!.Trim();
        if (!SectionBandTitles.Contains(label))
            return false;

        for (var i = 1; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
                return false;
        }

        return true;
    }

    public static string DisplayCell(string? value) => value ?? "—";
}
