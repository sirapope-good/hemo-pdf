using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>
/// Dynamic page number for designer canvas (typically <c>super-footer</c>).
/// Format tokens: <c>{current}</c>, <c>{total}</c> — default <c>{current} / {total}</c>.
/// </summary>
public static class ConfigurablePageOfComposer
{
    private const Unit Mm = Unit.Millimetre;
    public const string DefaultFormat = "{current} / {total}";

    public static void Compose(IContainer container, HprpDesignerElement element)
    {
        var chrome = element.Chrome;
        var hasBorder = !string.IsNullOrWhiteSpace(chrome?.Border)
            && !string.Equals(chrome.Border, "none", StringComparison.OrdinalIgnoreCase);
        var bw = hasBorder
            ? HprpChrome.ResolveBorderWidth(chrome)
            : 0f;
        var align = (element.Align ?? "center").Trim().ToLowerInvariant();
        var format = string.IsNullOrWhiteSpace(element.Text) ? DefaultFormat : element.Text!.Trim();

        var box = container
            .Height(Math.Max(3f, element.Box.HMm), Mm)
            .PaddingHorizontal(2f)
            .AlignMiddle();

        if (hasBorder)
            box = box.Border(bw);

        var fill = HprpChrome.FileHeaderFillOrNull(chrome);
        if (!string.IsNullOrWhiteSpace(fill))
            box = box.Background(fill);

        box = align switch
        {
            "left" => box.AlignLeft(),
            "right" => box.AlignRight(),
            _ => box.AlignCenter(),
        };

        var style = ThaiUrText.Base;
        if (chrome?.FontSize is > 0 and < 48)
            style = style.FontSize(chrome.FontSize.Value);
        else
            style = style.FontSize(8);

        // QuestPDF page tokens — split format around placeholders.
        box.Text(text =>
        {
            text.DefaultTextStyle(style);
            AppendFormatted(text, format);
        });
    }

    private static void AppendFormatted(TextDescriptor text, string format)
    {
        var remaining = format;
        while (remaining.Length > 0)
        {
            var iCur = remaining.IndexOf("{current}", StringComparison.OrdinalIgnoreCase);
            var iTot = remaining.IndexOf("{total}", StringComparison.OrdinalIgnoreCase);
            int next;
            string token;
            if (iCur < 0 && iTot < 0)
            {
                text.Span(remaining);
                return;
            }

            if (iCur >= 0 && (iTot < 0 || iCur < iTot))
            {
                next = iCur;
                token = "{current}";
            }
            else
            {
                next = iTot;
                token = "{total}";
            }

            if (next > 0)
                text.Span(remaining[..next]);

            if (token.Equals("{current}", StringComparison.OrdinalIgnoreCase))
                text.CurrentPageNumber();
            else
                text.TotalPages();

            remaining = remaining[(next + token.Length)..];
        }
    }
}
