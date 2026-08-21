using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class DataGridSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not IDataGridSource source || source.Grid is not { } grid)
        {
            return;
        }

        if (grid.ColumnHeaders.Count == 0)
        {
            return;
        }

        var columnCount = grid.ColumnHeaders.Count;
        var weights = ResolveColumnWeights(grid, columnCount);
        var chrome = grid.Chrome;
        var border = HprpChrome.ResolveBorderWidth(chrome);
        var headerFill = HprpChrome.ResolveHeaderFill(chrome, context, PdfSectionMetrics.SectionHeaderBackground);
        var fontSize = HprpChrome.ResolveFontSize(chrome, PdfStyleDefaults.Body.DataFontSize);
        var rowHeightMm = chrome?.RowHeightMm;

        container.Border(border).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn(weights[i]);
                }
            });

            if (!string.IsNullOrWhiteSpace(grid.Title))
            {
                var title = table.Cell().ColumnSpan((uint)columnCount)
                    .Background(headerFill)
                    .Border(border)
                    .Padding(PdfSectionMetrics.SectionTitlePadding);
                title.Text(grid.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            foreach (var header in grid.ColumnHeaders)
            {
                table.Cell().Border(border).Background(headerFill).Padding(PdfSectionMetrics.CellPadding)
                    .Text(header)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(fontSize)
                    .SemiBold();
            }

            foreach (var row in grid.Rows)
            {
                for (var i = 0; i < columnCount; i++)
                {
                    var value = i < row.Count ? row[i] : null;
                    var cell = table.Cell().Border(border).Padding(PdfSectionMetrics.CellPadding);
                    if (rowHeightMm is > 0)
                        cell = cell.MinHeight(rowHeightMm.Value, Unit.Millimetre);

                    cell.Text(value ?? "—")
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(fontSize);
                }
            }
        });
    }

    private static IReadOnlyList<float> ResolveColumnWeights(DataGridModel grid, int columnCount)
    {
        if (grid.ColumnWeights.Count == columnCount)
        {
            return grid.ColumnWeights;
        }

        return Enumerable.Repeat(1f, columnCount).ToList();
    }
}
