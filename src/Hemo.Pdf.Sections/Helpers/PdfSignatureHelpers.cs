using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

public static class PdfSignatureHelpers
{
    public static void RenderSignatureBlock(
        ColumnDescriptor column,
        SignatureInfo signature,
        string label,
        bool includeDate = true)
    {
        column.Spacing(2);

        column.Item().Row(titleRow =>
        {
            titleRow.RelativeItem().AlignLeft().AlignBottom().Text(label)
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.DataFontSize)
                .SemiBold();
        });

        column.Item().Row(contentRow =>
        {
            contentRow.RelativeItem().Height(32).AlignMiddle().AlignCenter().Element(container =>
            {
                if (signature.ImageBytes is { Length: > 0 } imageBytes)
                {
                    container.Height(24).Image(imageBytes).FitHeight();
                }
                else
                {
                    container.Height(24);
                }
            });
        });

        column.Item().PaddingHorizontal(12).Height(3).Element(line =>
        {
            const int segments = 12;

            line.Row(row =>
            {
                for (var i = 0; i < segments; i++)
                {
                    row.RelativeItem().Height(0.4f).Background(Colors.Black);
                    row.RelativeItem().Height(0.4f);
                }
            });
        });

        var displayName = string.IsNullOrWhiteSpace(signature.SignerName)
            ? "—"
            : signature.SignerName.Trim();

        column.Item().AlignCenter().PaddingTop(1).Text($"( {displayName} )")
            .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
            .FontSize(PdfStyleDefaults.Body.DataFontSize);

        if (!string.IsNullOrWhiteSpace(signature.SignerRole))
        {
            column.Item().AlignCenter().Text(signature.SignerRole)
                .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                .FontSize(PdfStyleDefaults.Footer.TextFontSize);
        }

        if (includeDate)
        {
            column.Item().AlignCenter().Text(PdfTextHelpers.FormatDate(signature.SignedAt))
                .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                .FontSize(PdfStyleDefaults.Footer.TextFontSize);
        }
    }
}
