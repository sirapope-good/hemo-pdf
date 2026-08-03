using Hemo.Pdf.Sections.Helpers;
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

    /// <summary>
    /// Header fill for a section already wrapped in <c>Border</c> - avoids double box around the title.
    /// </summary>
    public static void BlockHeader(this IContainer c, string text)
    {
        c.BorderBottom(HemosheetThaiUrStyle.BorderWidth)
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

    /// <summary>Like <see cref="Value"/> but empty/whitespace stays blank (no "-").</summary>
    public static void ValueBlank(this IContainer c, string? text)
    {
        c.PaddingLeft(1f).AlignMiddle().Text(string.IsNullOrWhiteSpace(text) ? "" : text).Style(Base);
    }

    public static void ValueCentered(this IContainer c, string? text)
    {
        c.AlignMiddle().AlignCenter().Text(string.IsNullOrWhiteSpace(text) ? "" : text).Style(Base);
    }

    public static void Checkbox(this RowDescriptor row, bool isChecked, float sizePt = 8f)
        => PdfCheckbox.Render(row, isChecked, sizePt);

    /// <summary>
    /// Label | Y+box | N+box with fixed mm columns (Telerik BottomLeftPanel: 25 | 16 | 17).
    /// </summary>
    public static void YesNo(
        this IContainer c,
        string label,
        bool? yes,
        float labelMm = 25f,
        float yColMm = 16f,
        float nColMm = 17f)
    {
        c.Row(r =>
        {
            r.ConstantItem(labelMm, Unit.Millimetre).AlignMiddle().PaddingLeft(1f).Text(label).Style(Base);
            YnCell(r, yColMm, "Y", yes == true);
            YnCell(r, nColMm, "N", yes == false);
        });
    }

    private static void YnCell(RowDescriptor row, float widthMm, string letter, bool isChecked)
    {
        row.ConstantItem(widthMm, Unit.Millimetre).AlignMiddle().Row(yn =>
        {
            yn.ConstantItem(2f);
            yn.AutoItem().AlignMiddle().Text(letter).Style(Base);
            yn.ConstantItem(3f);
            yn.Checkbox(isChecked, sizePt: 7f);
            yn.RelativeItem();
        });
    }

    /// <summary>Checkbox + label. Parent supplies row height (fixed or ExtendVertical + MinHeight).</summary>
    public static void CheckLine(this IContainer c, string label, bool isChecked)
    {
        c.AlignMiddle().Row(r =>
        {
            r.Checkbox(isChecked, sizePt: 6.5f);
            r.ConstantItem(2f);
            r.RelativeItem().AlignMiddle().Text(label).Style(Base);
        });
    }
}
