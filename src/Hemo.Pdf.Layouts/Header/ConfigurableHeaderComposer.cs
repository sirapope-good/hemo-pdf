using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Header;

/// <summary>QuestPDF renderer for <see cref="HprpHeaderLayoutModel"/> (designer config-header).</summary>
public static class ConfigurableHeaderComposer
{
    private const Unit Mm = Unit.Millimetre;

    public static void Compose(IContainer container, HprpHeaderLayoutModel model)
    {
        var preset = model.Preset;
        var bw = BorderWidth(preset.Chrome);

        container.Column(col =>
        {
            col.Item().Height(model.TitleRowHeightMm, Mm).Row(row =>
            {
                foreach (var band in preset.Columns)
                {
                    var cell = PlaceBand(row, band);
                    var kind = band.Kind.Trim().ToLowerInvariant();
                    switch (kind)
                    {
                        case HprpHeaderColumnKinds.Logo:
                            cell.Border(bw).ExtendVertical().AlignMiddle().AlignCenter()
                                .Element(c => DrawLogo(c, model));
                            break;
                        case HprpHeaderColumnKinds.Title:
                            cell.Border(bw).ExtendVertical().AlignMiddle().AlignCenter()
                                .Text(model.TitleText).Style(ThaiUrText.Title);
                            break;
                        case HprpHeaderColumnKinds.Meta:
                            cell.Border(bw).ExtendVertical().PaddingHorizontal(1.5f).AlignTop()
                                .Column(meta => DrawMeta(meta, model, bw));
                            break;
                        default:
                            cell.Border(bw).ExtendVertical().AlignMiddle().AlignCenter()
                                .Text(band.Id).Style(ThaiUrText.Base);
                            break;
                    }
                }
            });

            col.Item().Height(model.BottomRowHeightMm, Mm).Row(row =>
            {
                if (model.ShowDateAndHdNo)
                {
                    row.RelativeItem(3).Border(bw).ExtendVertical().PaddingHorizontal(1.5f).AlignMiddle()
                        .Row(r => DrawBottomFields(r, model));
                    row.RelativeItem(1).Border(bw).ExtendVertical().PaddingHorizontal(1.5f).AlignMiddle()
                        .Row(r =>
                        {
                            r.ConstantItem(22, Mm).AlignMiddle().Text("Date").Style(ThaiUrText.Bold);
                            r.RelativeItem().AlignMiddle()
                                .Text(Blank(model.DateText)).Style(ThaiUrText.Base);
                            r.ConstantItem(12, Mm).AlignMiddle().Text("HD NO.").Style(ThaiUrText.Bold);
                            r.ConstantItem(14, Mm).AlignMiddle()
                                .Text(Blank(model.HdNoText)).Style(ThaiUrText.Base);
                        });
                }
                else
                {
                    row.RelativeItem().Border(bw).ExtendVertical().PaddingHorizontal(1.5f).AlignMiddle()
                        .Row(r => DrawBottomFields(r, model));
                }
            });
        });
    }

    private static IContainer PlaceBand(RowDescriptor row, HprpHeaderBandColumn band)
    {
        if (band.WidthMm is > 0)
            return row.ConstantItem(band.WidthMm.Value, Mm);
        return row.RelativeItem(Math.Max(0.1f, band.Weight));
    }

    private static void DrawMeta(ColumnDescriptor meta, HprpHeaderLayoutModel model, float bw)
    {
        _ = bw;
        var lineH = model.TitleRowHeightMm / Math.Max(1, model.MetaLines.Count);
        foreach (var line in model.MetaLines)
        {
            meta.Item().Height(lineH, Mm).AlignMiddle().Row(r =>
            {
                r.ConstantItem(22, Mm).AlignMiddle().Text(line.Label).Style(ThaiUrText.Bold);
                r.RelativeItem().AlignMiddle()
                    .Text(Blank(line.Value)).Style(ThaiUrText.Base);
                if (!string.IsNullOrWhiteSpace(line.Label2))
                {
                    r.ConstantItem(12, Mm).AlignMiddle().Text(line.Label2).Style(ThaiUrText.Bold);
                    r.ConstantItem(14, Mm).AlignMiddle()
                        .Text(Blank(line.Value2)).Style(ThaiUrText.Base);
                }
            });
        }
    }

    private static void DrawBottomFields(RowDescriptor r, HprpHeaderLayoutModel model)
    {
        foreach (var field in model.BottomFields)
        {
            // Label-only tokens (e.g. T/Wk) with tiny weight and empty bind
            if (string.IsNullOrWhiteSpace(field.Value) && field.Weight < 0.05f)
            {
                r.ConstantItem(12, Mm).AlignMiddle().Text(field.Label).Style(ThaiUrText.Bold);
                continue;
            }

            r.ConstantItem(Math.Min(22, 8 + field.Label.Length), Mm).AlignMiddle()
                .Text(field.Label).Style(ThaiUrText.Bold);
            r.RelativeItem(Math.Max(0.1f, field.Weight)).AlignMiddle()
                .Text(Blank(field.Value)).Style(ThaiUrText.Base);
        }
    }

    private static void DrawLogo(IContainer c, HprpHeaderLayoutModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.LogoBase64))
        {
            try
            {
                var raw = model.LogoBase64!;
                var comma = raw.IndexOf(',');
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    raw = raw[(comma + 1)..];
                c.Image(Convert.FromBase64String(raw)).FitArea();
                return;
            }
            catch
            {
                // fall through
            }
        }

        c.Text(model.LogoFallbackText).Style(ThaiUrText.Base);
    }

    private static string Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "\u00A0" : value;

    private static float BorderWidth(HprpChrome? chrome) =>
        string.IsNullOrWhiteSpace(chrome?.Border)
            ? HemosheetThaiUrStyle.BorderWidth
            : HprpChrome.ResolveBorderWidth(chrome);
}
