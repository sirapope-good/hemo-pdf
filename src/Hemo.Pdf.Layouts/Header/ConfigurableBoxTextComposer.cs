using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>Simple banner / label box for designer canvas (co-pay title, etc.).</summary>
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
        var text = ResolveText(element, data);
        var align = (element.Align ?? "center").Trim().ToLowerInvariant();

        var box = container
            .Border(bw)
            .Background(fill)
            .Height(Math.Max(3f, element.Box.HMm), Mm)
            .PaddingHorizontal(2f)
            .AlignMiddle();

        box = align switch
        {
            "left" => box.AlignLeft(),
            "right" => box.AlignRight(),
            _ => box.AlignCenter(),
        };

        var style = ThaiUrText.Bold;
        if (chrome?.FontSize is > 0 and < 48)
            style = style.FontSize(chrome.FontSize.Value);

        box.Text(string.IsNullOrWhiteSpace(text) ? "\u00A0" : text).Style(style);
    }

    public static string ResolveText(HprpDesignerElement element, JsonElement? data)
    {
        if (!string.IsNullOrWhiteSpace(element.Bind) && data is JsonElement root && root.ValueKind == JsonValueKind.Object)
        {
            var path = element.Bind.Trim();
            if (!path.StartsWith("$", StringComparison.Ordinal))
                path = "$." + path.TrimStart('.');
            var selected = Hemo.Pdf.Core.Hprp.HprpJsonPath.Select(root, path);
            var fromBind = Hemo.Pdf.Core.Hprp.HprpJsonPath.AsString(selected);
            if (!string.IsNullOrWhiteSpace(fromBind))
                return fromBind!;
        }

        return element.Text ?? "";
    }
}
