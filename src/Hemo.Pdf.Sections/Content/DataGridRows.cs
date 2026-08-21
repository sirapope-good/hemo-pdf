namespace Hemo.Pdf.Sections.Content;

public static class DataGridRows
{
    public static bool IsSectionBand(IReadOnlyList<string?> row)
    {
        if (row is null || row.Count < 2 || string.IsNullOrWhiteSpace(row[0]))
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
