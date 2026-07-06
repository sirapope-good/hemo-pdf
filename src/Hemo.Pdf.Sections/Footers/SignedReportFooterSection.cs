using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Footers;

public sealed class SignedReportFooterSection : IReportFooterSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
        var footer = context.Branding?.Footer;

        container.Column(column =>
        {
            column.Spacing(1);

            if (signatures.Count > 0)
            {
                column.Item().Border(0.5f).PaddingBottom(2).Row(row =>
                {
                    foreach (var signature in signatures)
                    {
                        row.RelativeItem().Padding(4).MinHeight(55).Column(col =>
                        {
                            var label = string.IsNullOrWhiteSpace(signature.SignerRole)
                                ? "Signed by"
                                : signature.SignerRole;
                            PdfSignatureHelpers.RenderSignatureBlock(col, signature, label, includeDate: true);
                        });
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(footer?.DisclaimerText))
            {
                column.Item().Text(footer.DisclaimerText)
                    .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                    .FontSize(PdfStyleDefaults.Footer.TextFontSize);
            }

            column.Item().AlignRight().Text(text =>
            {
                text.CurrentPageNumber()
                    .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                    .FontSize(PdfStyleDefaults.Footer.TextFontSize);
                text.Span(" / ")
                    .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                    .FontSize(PdfStyleDefaults.Footer.TextFontSize);
                text.TotalPages()
                    .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                    .FontSize(PdfStyleDefaults.Footer.TextFontSize);
            });
        });
    }
}
