using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Content;

internal static class ReportBlockAdapters
{
    internal sealed class KeyValueRowsAdapter(string? title, IReadOnlyList<LabelValue> rows) : IKeyValueRowsSource
    {
        public string? SectionTitle => title;

        public IReadOnlyList<KeyValuePair<string, string?>> Rows =>
            rows.Select(r => new KeyValuePair<string, string?>(r.Label, r.Value)).ToList();
    }

    internal sealed class DataGridAdapter(DataGridReportBlock block) : IDataGridSource
    {
        public DataGridModel? Grid { get; } = new()
        {
            Title = block.Title,
            ColumnHeaders = block.Columns.ToList(),
            ColumnWeights = block.ColumnWeights.ToList(),
            Rows = block.Rows.Select(row => row.Select(v => (string?)v).ToList()).ToList(),
        };
    }

    internal sealed class FieldGridAdapter(FieldGridReportBlock block) : IFieldGridSource
    {
        public FieldGridModel? Grid { get; } = new()
        {
            Title = block.Title,
            Columns = block.Columns,
            Fields = block.Fields.Select(f => new FieldGridItem
            {
                Label = f.Label,
                Value = f.Value,
                ColumnSpan = f.ColumnSpan,
            }).ToList(),
        };
    }
}
