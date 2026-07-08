using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class PrePostHdNotesSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not PrePostHdNotesReportBlock block)
        {
            return;
        }

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            ComposeNoteRow(table, "Pre HD", block.PreHdContent, block.PreHdSigner);
            ComposeNoteRow(table, "Post HD", block.PostHdContent, block.PostHdSigner);
        });
    }

    private static void ComposeNoteRow(TableDescriptor table, string label, string? content, string? signer)
    {
        table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(col =>
        {
            col.Item().Text(label).SemiBold();
            col.Item().Text(string.IsNullOrWhiteSpace(content) ? "—" : content);
        });

        table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).AlignBottom()
            .Text(signer ?? "—");
    }
}
