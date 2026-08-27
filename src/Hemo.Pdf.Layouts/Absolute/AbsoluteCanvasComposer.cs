using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Absolute;

/// <summary>
/// Experimental freeform composer: widgets at absolute mm via QuestPDF Layers.
/// Composition path is untouched.
/// </summary>
public static class AbsoluteCanvasComposer
{
    public static QuestLayout Compose(AbsoluteCanvasViewModel vm, PdfReportContext context)
    {
        _ = context;
        var pageW = vm.Landscape ? 297f : 210f;
        var pageH = vm.Landscape ? 210f : 297f;

        return new QuestLayout
        {
            Landscape = vm.Landscape,
            MarginTop = 0,
            MarginBottom = 0,
            MarginLeft = 0,
            MarginRight = 0,
            MarginMillimeters = 0,
            Header = null,
            Footer = _ => { },
            Content = c => ComposePage(c, vm, pageW, pageH),
        };
    }

    private static void ComposePage(IContainer container, AbsoluteCanvasViewModel vm, float pageW, float pageH)
    {
        var originX = vm.Page.Left;
        var originY = vm.Page.Top;
        _ = pageW;
        _ = pageH;

        // Content already fills the page slot from QuestPdfRenderer — do not force A4 size again.
        container.Layers(layers =>
        {
            layers.PrimaryLayer().Element(e => e.Background(Colors.White));

            foreach (var widget in vm.Widgets)
            {
                var x = Math.Max(0, originX + widget.XMm);
                var y = Math.Max(0, originY + widget.YMm);
                var w = Math.Max(1f, widget.WMm);
                var h = Math.Max(1f, widget.HMm);

                layers.Layer()
                    .Width(w, Unit.Millimetre)
                    .Height(h, Unit.Millimetre)
                    .TranslateX(x, Unit.Millimetre)
                    .TranslateY(y, Unit.Millimetre)
                    .Element(box => DrawWidget(box, widget));
            }
        });
    }

    private static void DrawWidget(IContainer container, HprpAbsoluteWidget widget)
    {
        var boxed = ApplyChrome(container, widget.Style);
        var type = widget.Type?.Trim().ToLowerInvariant() ?? "text";

        switch (type)
        {
            case "frame":
                boxed.Border(widget.Style?.BorderWidth is > 0 ? widget.Style.BorderWidth.Value : 0.4f)
                    .BorderColor(ParseColor(widget.Style?.BorderColor, Colors.Grey.Medium))
                    .Padding(2)
                    .AlignTop()
                    .Text(AbsoluteCanvasViewModel.DataString(widget.Data, "label", "Frame"))
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium)
                    .FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily);
                break;

            case "table":
                DrawTable(boxed, widget.Data);
                break;

            default:
                DrawText(boxed, widget.Data);
                break;
        }
    }

    private static IContainer ApplyChrome(IContainer container, HprpAbsoluteWidgetStyle? style)
    {
        var c = container;
        if (!string.IsNullOrWhiteSpace(style?.BackgroundColor)
            && !string.Equals(style.BackgroundColor, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            c = c.Background(ParseColor(style.BackgroundColor, Colors.White));
        }

        if (style?.BorderWidth is > 0)
        {
            c = c.Border(style.BorderWidth.Value)
                .BorderColor(ParseColor(style.BorderColor, Colors.Grey.Lighten1));
        }

        return c.Padding(2);
    }

    private static void DrawText(IContainer container, JsonElement data)
    {
        var title = AbsoluteCanvasViewModel.DataString(data, "title");
        var content = AbsoluteCanvasViewModel.DataString(data, "content");
        var style = AbsoluteCanvasViewModel.DataString(data, "style", "body");

        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                col.Item().Text(title)
                    .SemiBold()
                    .FontSize(style == "title" ? 14 : 10)
                    .FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily);
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                col.Item().Text(content)
                    .FontSize(style == "title" ? 12 : 9)
                    .FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily);
            }
        });
    }

    private static void DrawTable(IContainer container, JsonElement data)
    {
        var headers = ReadStringArray(data, "headers");
        var rows = ReadRows(data);

        container.Table(table =>
        {
            var cols = Math.Max(1, headers.Count);
            table.ColumnsDefinition(def =>
            {
                for (var i = 0; i < cols; i++)
                    def.RelativeColumn();
            });

            if (headers.Count > 0)
            {
                table.Header(h =>
                {
                    foreach (var header in headers)
                    {
                        h.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Medium).Padding(2)
                            .Text(header).SemiBold().FontSize(8)
                            .FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily);
                    }
                });
            }

            foreach (var row in rows)
            {
                for (var i = 0; i < cols; i++)
                {
                    var cell = i < row.Count ? row[i] : "";
                    table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                        .Text(cell).FontSize(8)
                        .FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily);
                }
            }
        });
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement data, string name)
    {
        if (data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty(name, out var el)
            || el.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? (x.GetString() ?? "") : x.ToString())
            .ToList();
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadRows(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("rows", out var el)
            || el.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        var rows = new List<IReadOnlyList<string>>();
        foreach (var row in el.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
                continue;
            rows.Add(row.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.String ? (x.GetString() ?? "") : x.ToString())
                .ToList());
        }

        return rows;
    }

    private static string ParseColor(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;
        var t = hex.Trim();
        if (t.StartsWith('#') && (t.Length == 7 || t.Length == 4))
            return t;
        return fallback;
    }
}
