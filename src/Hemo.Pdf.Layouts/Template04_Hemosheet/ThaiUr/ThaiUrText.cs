using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Shared styling primitives for the ThaiUR Hemosheet form. Centralizing them here keeps the
/// large <see cref="ThaiUrHemosheetForm"/> readable and lets us tune the whole form in one place.
/// </summary>
internal static class ThaiUrText
{
    // Microsoft Sans Serif for Latin; QuestPDF's automated glyph fallback resolves Thai glyphs
    // from the registered Sarabun font when Microsoft Sans Serif lacks them.
    public static TextStyle Base => TextStyle.Default
        .FontFamily(HemosheetThaiUrStyle.FontFamily)
        .FontSize(HemosheetThaiUrStyle.BaseFontSize)
        .LineHeight(1f)
        .FontColor(Colors.Black);

    public static TextStyle Bold => Base.Bold();

    public static TextStyle UnitText => Base.FontSize(HemosheetThaiUrStyle.UnitFontSize);

    public static TextStyle Title => Base.FontSize(HemosheetThaiUrStyle.TitleFontSize).Bold();

    /// <summary>Border thickness helper so every cell uses the Telerik 0.4pt rule.</summary>
    public static IContainer Cell(this IContainer c) => c.Border(HemosheetThaiUrStyle.BorderWidth);

    /// <summary>A lavender section-title bar identical to the Telerik header cells.</summary>
    public static void HeaderBar(this IContainer c, string text)
    {
        c.Border(HemosheetThaiUrStyle.BorderWidth)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .AlignMiddle()
            .AlignCenter()
            .Text(text)
            .Style(Bold);
    }

    /// <summary>Left-aligned label text used across the form.</summary>
    public static void Label(this IContainer c, string text)
    {
        c.PaddingLeft(1f).AlignMiddle().Text(text).Style(Base);
    }

    public static void LabelBold(this IContainer c, string text)
    {
        c.PaddingLeft(1f).AlignMiddle().Text(text).Style(Bold);
    }

    public static void Value(this IContainer c, string? text)
    {
        c.PaddingLeft(1f).AlignMiddle().Text(string.IsNullOrWhiteSpace(text) ? "-" : text).Style(Base);
    }

    public static void ValueCentered(this IContainer c, string? text)
    {
        c.AlignMiddle().AlignCenter().Text(string.IsNullOrWhiteSpace(text) ? "" : text).Style(Base);
    }

    /// <summary>A small square checkbox (checked draws a slash), matching the Telerik checkbox image.</summary>
    public static void Checkbox(this RowDescriptor row, bool isChecked, float sizePt = 8f)
    {
        var box = row.ConstantItem(sizePt)
            .Height(sizePt)
            .Width(sizePt)
            .Border(HemosheetThaiUrStyle.BorderWidth)
            .AlignMiddle()
            .AlignCenter();

        box.Text(isChecked ? "\u2713" : "\u200B")
            .FontFamily("Arial")
            .FontSize(sizePt * 0.9f)
            .Bold();
    }

    /// <summary>Label + Y[ ] N[ ] triple used all over the assessment/vascular blocks.</summary>
    public static void YesNo(this IContainer c, string label, bool? yes)
    {
        c.Row(r =>
        {
            r.RelativeItem().Label(label);
            r.AutoItem().AlignMiddle().Text("Y").Style(Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == true);
            r.ConstantItem(4f);
            r.AutoItem().AlignMiddle().Text("N").Style(Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == false);
        });
    }

    /// <summary>Checkbox + caption, used in the bottom Complication/Nursing/Health groups.</summary>
    public static void CheckLine(this IContainer c, string label, bool isChecked)
    {
        c.Height(HemosheetThaiUrStyle.RowHeightMm, Unit.Millimetre).Row(r =>
        {
            r.Checkbox(isChecked);
            r.ConstantItem(2f);
            r.RelativeItem().AlignMiddle().Text(label).Style(Base);
        });
    }
}
