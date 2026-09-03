using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Sections;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>Word-lite multi-paragraph block for designer <c>narrative</c>.</summary>
public static class ConfigurableNarrativeComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float DefaultParagraphSpacingMm = 3.5f;
    private const float SubIndentMm = 6f;

    public static void Compose(
        IContainer container,
        HprpDesignerElement element,
        JsonElement? data)
    {
        var paragraphs = HprpNarrativeParagraphs.Resolve(element, data);
        var chrome = element.Chrome;
        var bw = string.IsNullOrWhiteSpace(chrome?.Border)
            ? 0f
            : HprpChrome.ResolveBorderWidth(chrome);
        var fill = HprpChrome.FileHeaderFillOrNull(chrome);
        var fontSize = chrome?.FontSize is > 0 and < 48
            ? chrome.FontSize.Value
            : 11f;
        var spacing = chrome?.RowHeightMm is > 0
            ? chrome.RowHeightMm.Value
            : DefaultParagraphSpacingMm;

        var box = container;
        if (bw > 0)
            box = box.Border(bw);
        if (!string.IsNullOrWhiteSpace(fill))
            box = box.Background(fill!);

        box = box
            .Padding(NarrativeLayout.FramePaddingMm * 0.55f, Mm)
            .AlignTop();

        if (paragraphs.Count == 0)
        {
            box.Text("(empty narrative)").FontSize(8).FontColor("#888888");
            return;
        }

        box.Column(col =>
        {
            col.Spacing(spacing, Mm);
            foreach (var para in paragraphs)
            {
                col.Item().Element(c => DrawParagraph(c, para, fontSize));
            }
        });
    }

    private static void DrawParagraph(IContainer container, HprpNarrativeParagraph para, float fontSize)
    {
        var role = (para.Role ?? "body").Trim().ToLowerInvariant();
        var align = (para.Align ?? (role == "title" ? "center" : "left")).Trim().ToLowerInvariant();
        var size = role switch
        {
            "title" => Math.Max(fontSize, 13f),
            "note" => Math.Max(8f, fontSize - 1.5f),
            _ => fontSize,
        };

        var text = para.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
            text = "\u00A0";

        IContainer box = container;
        if (para.Sub)
            box = box.PaddingLeft(SubIndentMm, Mm);

        box = align switch
        {
            "center" => box.AlignCenter(),
            "right" => box.AlignRight(),
            _ => box.AlignLeft(),
        };

        var style = role is "title"
            ? ThaiUrText.Bold.FontSize(size).LineHeight(NarrativeLayout.LineHeight)
            : ThaiUrText.Base.FontSize(size).LineHeight(NarrativeLayout.LineHeight);

        // Soft line breaks: split on \n so Studio/Word-like multi-line paragraphs work.
        var lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 1)
        {
            box.Text(lines[0]).Style(style);
            return;
        }

        box.Column(col =>
        {
            col.Spacing(1f, Mm);
            foreach (var line in lines)
            {
                col.Item().Text(string.IsNullOrEmpty(line) ? "\u00A0" : line).Style(style);
            }
        });
    }
}
