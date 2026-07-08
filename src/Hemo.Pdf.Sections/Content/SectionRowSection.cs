using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class SectionRowSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not SectionRowReportBlock block || block.Blocks.Count == 0)
        {
            return;
        }

        container.Row(row =>
        {
            foreach (var child in block.Blocks)
            {
                row.RelativeItem().PaddingHorizontal(1).Element(column =>
                    ComposeColumnChild(column, child, context));
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
