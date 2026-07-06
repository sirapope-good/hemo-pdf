using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Preview;

public static class DataGridPreviewMapper
{
    public static DataGridReportBlock? Map(object viewModel)
    {
        if (viewModel is not IDataGridSource source || source.Grid is not { } grid)
        {
            return null;
        }

        if (grid.ColumnHeaders.Count == 0)
        {
            return null;
        }

        return new DataGridReportBlock
        {
            Title = grid.Title,
            Columns = grid.ColumnHeaders,
            Rows = grid.Rows
                .Select(row => row
                    .Select(value => value ?? "—")
                    .ToList() as IReadOnlyList<string>)
                .ToList(),
        };
    }
}
