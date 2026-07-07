using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
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

        container.Border(0.5f).Table(table =>
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
                table.Cell().ColumnSpan((uint)columnCount)
                    .Background(PdfSectionMetrics.SectionHeaderBackground)
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(grid.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            foreach (var header in grid.ColumnHeaders)
            {
                table.Cell().Border(0.5f).Background(PdfSectionMetrics.SectionHeaderBackground).Padding(PdfSectionMetrics.CellPadding)
                    .Text(header)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize)
                    .SemiBold();
            }

            foreach (var row in grid.Rows)
            {
                for (var i = 0; i < columnCount; i++)
                {
                    var value = i < row.Count ? row[i] : null;
                    table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
                        .Text(value ?? "—")
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(PdfStyleDefaults.Body.DataFontSize);
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
