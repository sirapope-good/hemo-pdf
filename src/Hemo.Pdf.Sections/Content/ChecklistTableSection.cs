using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using Hemo.Pdf.Sections.Preview;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class ChecklistTableSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is ChecklistTableReportBlock reportBlock)
        {
            ComposeBlock(container, reportBlock, context);
            return;
        }

        if (viewModel is not IChecklistSource source || source.Checklist is not { } checklist)
        {
            return;
        }

        var block = ChecklistTablePreviewMapper.Map(checklist);
        if (block is null)
        {
            return;
        }

        ComposeBlock(container, block, context);
    }

    private static void ComposeBlock(
        IContainer container,
        ChecklistTableReportBlock block,
        PdfReportContext? context = null)
    {
        if (block.Rows.Count == 0)
        {
            return;
        }

        var layout = block.Layout ?? ChecklistTablePreviewMapper.LayoutDefault;
        var ynLayout = string.Equals(layout, ChecklistTablePreviewMapper.LayoutYnColumns, StringComparison.Ordinal);
        var preReLayout = string.Equals(layout, ChecklistTablePreviewMapper.LayoutPreReMatrix, StringComparison.Ordinal);
        var columnCount = (uint)Math.Max(1, block.Columns.Count);
        var headerBg = ReportSectionHeaderChrome.Resolve(context, PdfSectionMetrics.SectionHeaderBackground);

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                if (preReLayout)
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(18);
                    columns.ConstantColumn(18);
                    columns.ConstantColumn(18);
                    columns.ConstantColumn(18);
                    columns.RelativeColumn(2);
                }
                else if (ynLayout)
                {
                    columns.ConstantColumn(14);
                    columns.ConstantColumn(14);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                }
                else
                {
                    columns.ConstantColumn(16);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                }
            });

            if (!string.IsNullOrWhiteSpace(block.Title))
            {
                table.Cell().ColumnSpan(columnCount)
                    .Background(headerBg)
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(block.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            foreach (var header in block.Columns)
            {
                table.Cell().Border(0.5f).Background(headerBg)
                    .Padding(PdfSectionMetrics.CellPadding)
                    .Text(header)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize)
                    .SemiBold();
            }

            foreach (var row in block.Rows)
            {
                foreach (var cell in row)
                {
                    var cellContainer = table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding);
                    switch (cell)
                    {
                        case ChecklistCheckboxCell checkbox:
                            cellContainer.Row(rowDescriptor =>
                                PdfComponentHelpers.RenderCheckbox(rowDescriptor, checkbox.Checked, 8f));
                            break;
                        case ChecklistTextCell textCell:
                            cellContainer.Text(string.IsNullOrWhiteSpace(textCell.Text) ? "—" : textCell.Text)
                                .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                                .FontSize(PdfStyleDefaults.Body.DataFontSize);
                            break;
                    }
                }
            }
        });
    }
}
