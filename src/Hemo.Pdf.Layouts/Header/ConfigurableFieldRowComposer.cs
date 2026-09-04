using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Text.Json;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>
/// Designer <c>field-row</c>: checkbox option lists + fill-in text for paper-like forms.
/// Empty bind → unchecked / blank underline (printable blank report).
/// </summary>
public static class ConfigurableFieldRowComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const string CheckedMark = "[✓]";
    private const string UncheckedMark = "[ ]";

    public static void Compose(
        IContainer container,
        HprpDesignerElement element,
        JsonElement? data)
    {
        var chrome = element.Chrome;
        var bw = string.IsNullOrWhiteSpace(chrome?.Border)
            ? 0f
            : HprpChrome.ResolveBorderWidth(chrome);
        var fill = HprpChrome.FileHeaderFillOrNull(chrome) ?? Colors.White;

        var box = container
            .Border(bw)
            .Background(fill)
            .Height(Math.Max(3f, element.Box.HMm), Mm)
            .PaddingHorizontal(2f)
            .AlignMiddle();

        var fontSize = chrome?.FontSize is > 0 and < 48 ? chrome.FontSize.Value : 8f;
        var labelStyle = ThaiUrText.Base.FontSize(fontSize);
        var valueStyle = ThaiUrText.Bold.FontSize(fontSize);

        var segments = element.Segments;
        if (segments is not { Count: > 0 })
        {
            box.Text("\u00A0").Style(labelStyle);
            return;
        }

        box.Row(row =>
        {
            foreach (var segment in segments)
            {
                var flex = segment.Flex is > 0 ? segment.Flex.Value : 1f;
                var cell = row.RelativeItem(flex).AlignMiddle();
                cell = AlignContainer(cell, segment.Align ?? "left");
                var kind = (segment.Kind ?? HprpFieldRowSegmentKinds.Text).Trim().ToLowerInvariant();
                if (kind == HprpFieldRowSegmentKinds.Options)
                    ComposeOptions(cell, segment, data, labelStyle, valueStyle);
                else
                    ComposeText(cell, segment, data, labelStyle, valueStyle);
            }
        });
    }

    private static void ComposeOptions(
        IContainer cell,
        HprpFieldRowSegment segment,
        JsonElement? data,
        TextStyle labelStyle,
        TextStyle valueStyle)
    {
        var bound = ResolveBound(segment.Bind, data);
        var options = segment.Options ?? Array.Empty<HprpFieldOption>();
        if (options.Count == 0)
        {
            cell.Text(string.IsNullOrWhiteSpace(segment.Label) ? "\u00A0" : segment.Label).Style(labelStyle);
            return;
        }

        if (segment.Wrap)
        {
            var perLine = segment.OptionsPerLine is > 0 ? segment.OptionsPerLine.Value : 4;
            cell.Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(segment.Label))
                {
                    col.Item().Text(t =>
                    {
                        t.Span(segment.Label!.TrimEnd() + " ").Style(labelStyle);
                    });
                }

                for (var i = 0; i < options.Count; i += perLine)
                {
                    var chunk = options.Skip(i).Take(perLine).ToList();
                    col.Item().Text(t => WriteOptions(t, chunk, bound, labelStyle, valueStyle, leadingLabel: null));
                }
            });
            return;
        }

        cell.Text(t => WriteOptions(t, options, bound, labelStyle, valueStyle, segment.Label));
    }

    private static void WriteOptions(
        TextDescriptor t,
        IReadOnlyList<HprpFieldOption> options,
        string? bound,
        TextStyle labelStyle,
        TextStyle valueStyle,
        string? leadingLabel)
    {
        if (!string.IsNullOrWhiteSpace(leadingLabel))
            t.Span(leadingLabel!.TrimEnd() + " ").Style(labelStyle);

        for (var i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            var on = HprpFieldRowMatching.IsSelected(bound, opt);
            var mark = on ? CheckedMark : UncheckedMark;
            t.Span($"{mark} {HprpFieldRowMatching.DisplayLabel(opt)}").Style(on ? valueStyle : labelStyle);
            if (i < options.Count - 1)
                t.Span("  ").Style(labelStyle);
        }
    }

    private static void ComposeText(
        IContainer cell,
        HprpFieldRowSegment segment,
        JsonElement? data,
        TextStyle labelStyle,
        TextStyle valueStyle)
    {
        var value = ResolveBound(segment.Bind, data);
        if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(segment.Text))
            value = segment.Text!.Trim();

        cell.Text(t =>
        {
            if (!string.IsNullOrWhiteSpace(segment.Label))
                t.Span(segment.Label!.TrimEnd() + " ").Style(labelStyle);

            if (!string.IsNullOrWhiteSpace(value))
            {
                t.Span(value).Style(valueStyle);
                return;
            }

            if (segment.BlankLine)
                t.Span("........................").Style(labelStyle);
            else
                t.Span("\u00A0").Style(labelStyle);
        });
    }

    private static string? ResolveBound(string? bind, JsonElement? data)
    {
        if (string.IsNullOrWhiteSpace(bind) || data is not JsonElement root || root.ValueKind != JsonValueKind.Object)
            return null;
        var path = bind.Trim();
        if (!path.StartsWith("$", StringComparison.Ordinal))
            path = "$." + path.TrimStart('.');
        return Hemo.Pdf.Core.Hprp.HprpJsonPath.AsString(Hemo.Pdf.Core.Hprp.HprpJsonPath.Select(root, path));
    }

    private static IContainer AlignContainer(IContainer box, string align) =>
        align.Trim().ToLowerInvariant() switch
        {
            "left" => box.AlignLeft(),
            "right" => box.AlignRight(),
            _ => box.AlignCenter(),
        };
}
