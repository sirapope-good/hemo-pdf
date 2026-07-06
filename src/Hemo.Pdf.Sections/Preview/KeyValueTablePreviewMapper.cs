using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview;

public static class KeyValueTablePreviewMapper
{
    public static KeyValueTableReportBlock? Map(object viewModel)
    {
        IReadOnlyList<KeyValuePair<string, string?>> rows;
        string? title;

        switch (viewModel)
        {
            case Content.IKeyValueRowsSource source:
                rows = source.Rows;
                title = source.SectionTitle;
                break;
            case SimpleReportViewModel simple:
                rows = simple.Rows;
                title = string.IsNullOrWhiteSpace(simple.Title) ? null : simple.Title;
                break;
            default:
                return null;
        }

        if (rows.Count == 0)
        {
            return null;
        }

        return new KeyValueTableReportBlock
        {
            Title = title,
            Rows = rows
                .Select(row => new LabelValue
                {
                    Label = row.Key,
                    Value = string.IsNullOrWhiteSpace(row.Value) ? "—" : row.Value!,
                })
                .ToList(),
        };
    }
}
