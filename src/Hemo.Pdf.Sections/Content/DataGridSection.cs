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

        container.Column(col =>
        {
            col.Spacing(4);

            if (!string.IsNullOrWhiteSpace(grid.Title))
            {
                col.Item().Text(grid.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in grid.ColumnHeaders)
                    {
                        columns.RelativeColumn();
                    }
                });

                foreach (var header in grid.ColumnHeaders)
                {
                    table.Cell().Border(0.5f).Background("#f0f0f0").Padding(4)
                        .Text(header)
                        .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                        .FontSize(PdfStyleDefaults.Body.DataFontSize)
                        .SemiBold();
                }

                foreach (var row in grid.Rows)
                {
                    for (var i = 0; i < grid.ColumnHeaders.Count; i++)
                    {
                        var value = i < row.Count ? row[i] : null;
                        table.Cell().Border(0.5f).Padding(4)
                            .Text(value ?? "—")
                            .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                            .FontSize(PdfStyleDefaults.Body.DataFontSize);
                    }
                }
            });
        });
    }
}
