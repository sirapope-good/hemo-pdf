using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class FieldGridSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not IFieldGridSource source || source.Grid is not { } grid)
        {
            return;
        }

        if (grid.Fields.Count == 0)
        {
            return;
        }

        var columns = Math.Max(1, grid.Columns);

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                for (var i = 0; i < columns; i++)
                {
                    def.RelativeColumn();
                }
            });

            if (!string.IsNullOrWhiteSpace(grid.Title))
            {
                table.Cell().ColumnSpan((uint)columns)
                    .Background(ReportSectionHeaderChrome.Resolve(context, PdfSectionMetrics.SectionHeaderBackground))
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(grid.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            var index = 0;
            while (index < grid.Fields.Count)
            {
                var colUsed = 0;
                while (colUsed < columns && index < grid.Fields.Count)
                {
                    var field = grid.Fields[index];
                    var span = Math.Clamp(field.ColumnSpan, 1, columns - colUsed);
                    ComposeFieldCell(table, field, span);
                    colUsed += span;
                    index++;
                }

                while (colUsed < columns)
                {
                    table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Text("—")
                        .FontSize(PdfStyleDefaults.Body.DataFontSize);
                    colUsed++;
                }
            }
        });
    }

    private static void ComposeFieldCell(TableDescriptor table, FieldGridItem field, int span)
    {
        table.Cell().ColumnSpan((uint)span).Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
            .Text(text => PdfTextHelpers.ComposeInlineLabelValue(text, field.Label, field.Value));
    }
}
