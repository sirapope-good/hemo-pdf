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

            table.Header(header =>
            {
                if (!string.IsNullOrWhiteSpace(grid.Title))
                {
                    var title = header.Cell().ColumnSpan((uint)columnCount)
                        .Background(headerFill)
                        .Border(border)
                        .Padding(PdfSectionMetrics.SectionTitlePadding);
                    title.Text(grid.Title)
                        .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                        .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                        .SemiBold();
                }

                foreach (var columnHeader in grid.ColumnHeaders)
                {
                    var headerCell = header.Cell().Border(border).Background(headerFill);
                    if (rowHeightMm is > 0)
                        headerCell = headerCell.Height(rowHeightMm.Value, Unit.Millimetre);
                    headerCell.Padding(PdfSectionMetrics.CellPadding)
                        .Text(columnHeader)
                        .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                        .FontSize(fontSize)
                        .SemiBold();
                }
            });

            foreach (var row in grid.Rows)
            {
                var sectionBand = DataGridRows.IsSectionBand(row);
                for (var i = 0; i < columnCount; i++)
                {
                    var value = i < row.Count ? row[i] : null;
                    var cell = table.Cell().Border(border);
                    if (sectionBand)
                        cell = cell.Background(headerFill);
                    if (rowHeightMm is > 0)
                        cell = cell.Height(rowHeightMm.Value, Unit.Millimetre);
                    cell = cell.Padding(PdfSectionMetrics.CellPadding);

                    var text = cell.Text(sectionBand || value is not null ? value ?? "" : DataGridRows.DisplayCell(value));
                    text.FontFamily(sectionBand
                            ? PdfStyleDefaults.Body.SectionTitleFontFamily
                            : PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(fontSize);
                    if (sectionBand)
                        text.SemiBold();
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

        if (columnCount <= 0)
        {
            return [];
        }

        var weights = Enumerable.Repeat(1f, columnCount).ToArray();
        weights[0] = 3f;
        return weights;
    }
}
