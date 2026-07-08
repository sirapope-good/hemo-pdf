using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class SubHeaderBarSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not SubHeaderBarReportBlock block || block.Fields.Count == 0)
        {
            return;
        }

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            for (var i = 0; i < block.Fields.Count; i++)
            {
                var field = block.Fields[i];
                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Text(text =>
                {
                    text.Span($"{field.Label}: ").SemiBold();
                    text.Span(field.Value ?? "—");
                });
            }

            if (block.Fields.Count % 2 == 1)
            {
                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Text(" ");
            }
        });
    }
}
