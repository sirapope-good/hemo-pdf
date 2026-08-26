using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class SectionRowSection : IContentSection
{
    private const Unit Mm = Unit.Millimetre;

    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not SectionRowReportBlock block || block.Blocks.Count == 0)
        {
            return;
        }

        var parsed = HprpChrome.ParseRowCellWidths(block.ColumnWidths);
        var gap = block.GapMm is > 0 ? block.GapMm.Value : 0f;

        container.Row(row =>
        {
            for (var i = 0; i < block.Blocks.Count; i++)
            {
                IContainer slot;
                if (parsed.Count == block.Blocks.Count && parsed[i].ConstantMm)
                    slot = row.ConstantItem(parsed[i].Value, Mm);
                else if (parsed.Count == block.Blocks.Count)
                    slot = row.RelativeItem(parsed[i].Value);
                else
                    slot = row.RelativeItem();

                if (gap > 0)
                    slot = slot.PaddingHorizontal(gap / 2f, Mm);
                else
                    slot = slot.PaddingHorizontal(1);

                var child = block.Blocks[i];
                slot.Element(column => ComposeColumnChild(column, child, context));
            }
        });
    }

    private static void ComposeColumnChild(IContainer container, ReportBlock child, PdfReportContext context)
    {
        if (child is ColumnStackReportBlock stack)
        {
            container.Column(col =>
            {
                col.Spacing(PdfSectionMetrics.BlockSpacing);
                foreach (var item in stack.Blocks)
                {
                    col.Item().Element(c => ReportBlockPdfComposer.Compose(c, item, context));
                }
            });
            return;
        }

        ReportBlockPdfComposer.Compose(container, child, context);
    }
}
