using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Shared styling primitives for the ThaiUR Hemosheet form.
/// </summary>
internal static class ThaiUrText
{
    public static TextStyle Base => TextStyle.Default
        .FontFamily(HemosheetThaiUrStyle.FontFamily)
        .FontSize(HemosheetThaiUrStyle.BaseFontSize)
        .LineHeight(1f)
        .FontColor(Colors.Black);

    public static TextStyle Bold => Base.Bold();

    public static TextStyle UnitText => Base.FontSize(HemosheetThaiUrStyle.UnitFontSize);

    public static TextStyle Title => Base.FontSize(HemosheetThaiUrStyle.TitleFontSize).Bold();

    public static IContainer Cell(this IContainer c) => c.Border(HemosheetThaiUrStyle.BorderWidth);

    public static void HeaderBar(this IContainer c, string text)
    {
        c.Border(HemosheetThaiUrStyle.BorderWidth)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Unit.Millimetre)
            .AlignMiddle()
            .AlignCenter()
            .Text(text)
            .Style(Bold);
    }

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

    public static void Checkbox(this RowDescriptor row, bool isChecked, float sizePt = 8f)
    {
        var box = row.ConstantItem(sizePt)
            .Height(sizePt)
            .Width(sizePt)
            .Border(HemosheetThaiUrStyle.BorderWidth)
            .AlignMiddle()
            .AlignCenter();

        // Keep checkbox mark on Sarabun so Docker/Linux never depends on Arial.
        box.Text(isChecked ? "\u2713" : "\u200B")
            .FontFamily(HemosheetThaiUrStyle.FontFamily)
            .FontSize(sizePt * 0.85f)
            .Bold();
    }

    public static void YesNo(this IContainer c, string label, bool? yes)
    {
        c.Row(r =>
        {
            r.RelativeItem().AlignMiddle().PaddingLeft(1f).Text(label).Style(Base);
            r.AutoItem().AlignMiddle().Text("Y").Style(Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == true, sizePt: 7f);
            r.ConstantItem(3f);
            r.AutoItem().AlignMiddle().Text("N").Style(Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == false, sizePt: 7f);
        });
    }

    public static void CheckLine(this IContainer c, string label, bool isChecked)
    {
        c.Height(HemosheetThaiUrStyle.CheckRowHeightMm, Unit.Millimetre).AlignMiddle().Row(r =>
        {
            r.Checkbox(isChecked, sizePt: 6.5f);
            r.ConstantItem(2f);
            r.RelativeItem().AlignMiddle().Text(label).Style(Base);
        });
    }
}
