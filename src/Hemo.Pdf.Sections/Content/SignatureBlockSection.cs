using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class SignatureBlockSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
        if (signatures.Count == 0)
        {
            return;
        }

        container.PaddingTop(8).Row(row =>
        {
            foreach (var signature in signatures)
            {
                row.RelativeItem().Padding(4).MinHeight(60).Column(col =>
                {
                    var label = string.IsNullOrWhiteSpace(signature.SignerRole)
                        ? "ลายเซ็น"
                        : signature.SignerRole;
                    PdfSignatureHelpers.RenderSignatureBlock(col, signature, label, includeDate: true);
                });
            }
        });
    }
}
