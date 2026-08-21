using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Default;

/// <summary>Text helpers for clinical-03 Default form (neutral chrome).</summary>
public static class DefaultText
{
    public static TextStyle Base => TextStyle.Default
        .FontFamily(HemosheetDefaultStyle.FontFamily)
        .FontSize(HemosheetDefaultStyle.BaseFontSize)
        .LineHeight(1f)
        .FontColor(Colors.Black);

    public static TextStyle Bold => Base.Bold();

    public static TextStyle UnitText => Base.FontSize(HemosheetDefaultStyle.UnitFontSize);

    public static TextStyle Dialysis => Base.FontSize(HemosheetDefaultStyle.DialysisFontSize);

    public static TextStyle DialysisBold => Dialysis.Bold();

    public static TextStyle DialysisUnit => Dialysis.FontSize(HemosheetDefaultStyle.DialysisUnitFontSize);

    public static void HeaderBar(this IContainer c, string text)
    {
        c.Border(HemosheetDefaultStyle.BorderWidth)
            .Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
            .Height(HemosheetDefaultStyle.HeaderBarHeightMm, Unit.Millimetre)
            .AlignMiddle()
            .AlignCenter()
            .Text(text)
            .Style(Bold);
    }

    public static void Label(this IContainer c, string text) =>
        c.PaddingLeft(1f).AlignMiddle().Text(text).Style(Base);

    public static void LabelBold(this IContainer c, string text) =>
        c.PaddingLeft(1f).AlignMiddle().Text(text).Style(Bold);

    public static void Value(this IContainer c, string? text) =>
        c.PaddingLeft(1f).AlignMiddle().Text(string.IsNullOrWhiteSpace(text) ? "-" : text).Style(Base);

    /// <summary>Like <see cref="Value"/> but empty/whitespace stays blank (no "-").</summary>
    public static void ValueBlank(this IContainer c, string? text) =>
        c.PaddingLeft(1f).AlignMiddle().Text(string.IsNullOrWhiteSpace(text) ? "" : text).Style(Base);

    public static void ValueCentered(this IContainer c, string? text) =>
        c.AlignMiddle().AlignCenter().Text(string.IsNullOrWhiteSpace(text) ? "" : text).Style(Base);

    public static void Checkbox(this RowDescriptor row, bool isChecked, float sizePt = 8f) =>
        PdfCheckbox.Render(row, isChecked, sizePt);

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

    public static void YesNoValue(
        this IContainer c,
        string label,
        bool? yes,
        string? value,
        string unit,
        float labelMm = 18f,
        float yColMm = 10f,
        float nColMm = 10f,
        float unitMm = 12f)
    {
        c.Row(r =>
        {
            r.ConstantItem(labelMm, Unit.Millimetre).AlignMiddle().PaddingLeft(1f).Text(label).Style(Base);
            YnCell(r, yColMm, "Y", yes == true);
            YnCell(r, nColMm, "N", yes == false);
            r.RelativeItem().Value(value);
            r.ConstantItem(unitMm, Unit.Millimetre).AlignMiddle().Text(unit).Style(UnitText);
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
