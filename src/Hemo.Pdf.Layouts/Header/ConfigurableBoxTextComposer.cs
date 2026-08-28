using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>Banner / label / multi-value meta row for designer canvas.</summary>
public static class ConfigurableBoxTextComposer
{
    private const Unit Mm = Unit.Millimetre;

    public static void Compose(
        IContainer container,
        HprpDesignerElement element,
        JsonElement? data)
    {
        var chrome = element.Chrome;
        var bw = string.IsNullOrWhiteSpace(chrome?.Border)
            ? HemosheetThaiUrStyle.BorderWidth
            : HprpChrome.ResolveBorderWidth(chrome);
        var fill = HprpChrome.FileHeaderFillOrNull(chrome)
            ?? ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground);

        var box = container
            .Border(bw)
            .Background(fill)
            .Height(Math.Max(3f, element.Box.HMm), Mm)
            .PaddingHorizontal(2f)
            .AlignMiddle();

        var style = ThaiUrText.Bold;
        var labelStyle = ThaiUrText.Base;
        if (chrome?.FontSize is > 0 and < 48)
        {
            style = style.FontSize(chrome.FontSize.Value);
            labelStyle = labelStyle.FontSize(chrome.FontSize.Value);
        }

        if (element.Items is { Count: > 0 } items)
        {
            box.Row(row =>
            {
                foreach (var item in items)
                {
                    var flex = item.Flex is > 0 ? item.Flex.Value : 1f;
                    var cell = row.RelativeItem(flex).AlignMiddle();
                    cell = AlignContainer(cell, item.Align ?? "left");
                    cell.Text(t => WriteItemSpans(t, item, data, labelStyle, style));
                }
            });
            return;
        }

        var align = (element.Align ?? "center").Trim().ToLowerInvariant();
        box = AlignContainer(box, align);
        var text = ResolveText(element, data);
        box.Text(string.IsNullOrWhiteSpace(text) ? "\u00A0" : text).Style(style);
    }

    public static string ResolveText(HprpDesignerElement element, JsonElement? data)
    {
        if (element.Items is { Count: > 0 } items)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                    sb.Append("  ");
                sb.Append(FormatItemPlain(items[i], data));
            }

            return sb.ToString().Trim();
        }

        if (!string.IsNullOrWhiteSpace(element.Bind) && data is JsonElement root && root.ValueKind == JsonValueKind.Object)
        {
            var fromBind = ReadBind(root, element.Bind);
            if (!string.IsNullOrWhiteSpace(fromBind))
                return fromBind!;
        }

        return element.Text ?? "";
    }

    public static string FormatItemPlain(HprpBoxTextItem item, JsonElement? data)
    {
        var sb = new StringBuilder();
        AppendLabeledValue(sb, item.Label, ResolveItemValue(item.Bind, item.Text, data));
        AppendLabeledValue(sb, item.Label2, ResolveItemValue(item.Bind2, item.Text2, data));
        return sb.ToString().Trim();
    }

    private static void WriteItemSpans(
        TextDescriptor t,
        HprpBoxTextItem item,
        JsonElement? data,
        TextStyle labelStyle,
        TextStyle valueStyle)
    {
        WriteLabeledValue(t, item.Label, ResolveItemValue(item.Bind, item.Text, data), labelStyle, valueStyle);
        WriteLabeledValue(t, item.Label2, ResolveItemValue(item.Bind2, item.Text2, data), labelStyle, valueStyle);
        if (string.IsNullOrWhiteSpace(item.Label)
            && string.IsNullOrWhiteSpace(item.Label2)
            && string.IsNullOrWhiteSpace(ResolveItemValue(item.Bind, item.Text, data))
            && string.IsNullOrWhiteSpace(ResolveItemValue(item.Bind2, item.Text2, data)))
        {
            t.Span("\u00A0");
        }
    }

    private static void WriteLabeledValue(
        TextDescriptor t,
        string? label,
        string? value,
        TextStyle labelStyle,
        TextStyle valueStyle)
    {
        if (!string.IsNullOrWhiteSpace(label))
            t.Span(label.TrimEnd() + " ").Style(labelStyle);
        if (!string.IsNullOrWhiteSpace(value))
            t.Span(value).Style(valueStyle);
    }

    private static void AppendLabeledValue(StringBuilder sb, string? label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(label))
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(label.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(value);
        }
    }

    private static string? ResolveItemValue(string? bind, string? text, JsonElement? data)
    {
        if (!string.IsNullOrWhiteSpace(bind) && data is JsonElement root && root.ValueKind == JsonValueKind.Object)
        {
            var fromBind = ReadBind(root, bind);
            if (!string.IsNullOrWhiteSpace(fromBind))
                return fromBind;
        }

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? ReadBind(JsonElement root, string? bind)
    {
        if (string.IsNullOrWhiteSpace(bind))
            return null;
        var path = bind.Trim();
        if (!path.StartsWith("$", StringComparison.Ordinal))
            path = "$." + path.TrimStart('.');
        var selected = Hemo.Pdf.Core.Hprp.HprpJsonPath.Select(root, path);
        return Hemo.Pdf.Core.Hprp.HprpJsonPath.AsString(selected);
    }

    private static IContainer AlignContainer(IContainer box, string align) =>
        align.Trim().ToLowerInvariant() switch
        {
            "left" => box.AlignLeft(),
            "right" => box.AlignRight(),
            _ => box.AlignCenter(),
        };
}
