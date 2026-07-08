using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class ChecklistClusterSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not ChecklistClusterReportBlock block || block.Tables.Count == 0)
        {
            return;
        }

        container.Row(row =>
        {
            foreach (var table in block.Tables)
            {
                row.RelativeItem().PaddingHorizontal(1).Element(c =>
                    ReportBlockPdfComposer.Compose(c, table, context));
            }
        });
    }
}
