using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Thai UR #05 table: DATE | PROGRESS NOTE (SOAP) | ORDER FOR ONE DAY | ORDER FOR CONTINUATION.
/// About two session blocks per A4 page.
/// </summary>
public sealed class Clinical05SoapTableSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;
    internal const int MinEmptyRows = 2;

    public const string GoodConscious = "goodConscious";
    public const string Drowsiness = "drowsiness";
    public const string Other = "other";
    public const string Normal = "normal";
    public const string Abnormal = "abnormal";

    public void Compose(IContainer container, Clinical05ProgressNoteReportViewModel vm, float rowHeightMm)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(18, Mm);
                cols.RelativeColumn(2.4f);
                cols.RelativeColumn(1.1f);
                cols.RelativeColumn(1.1f);
            });

            HeaderCell(t, "DATE");
            HeaderCell(t, "PROGRESS NOTE");
            HeaderCell(t, "ORDER FOR ONE DAY");
            HeaderCell(t, "ORDER FOR CONTINUATION");

            var rows = vm.Sessions ?? [];
            var drawCount = Math.Max(rows.Count, MinEmptyRows);
            for (var i = 0; i < drawCount; i++)
            {
                var row = i < rows.Count ? rows[i] : null;
                t.Cell().Element(c => DateCell(c, row?.DateLabel, rowHeightMm));
                t.Cell().Element(c => SoapCell(c, row, rowHeightMm));
                t.Cell().Element(c => OrderCell(c, row?.OrderForOneDay, rowHeightMm));
                t.Cell().Element(c => OrderCell(c, row?.OrderForContinuation, rowHeightMm));
            }
        });
    }

    private static void HeaderCell(TableDescriptor t, string text)
    {
        t.Cell()
            .Border(Bw)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .Text(text)
            .Style(ThaiUrText.Bold);
    }

    private static void DateCell(IContainer c, string? dateLabel, float heightMm)
    {
        c.Border(Bw)
            .MinHeight(heightMm, Mm)
            .Padding(1.2f, Mm)
            .AlignTop()
            .AlignCenter()
            .Text(dateLabel ?? string.Empty)
            .Style(ThaiUrText.Base);
    }

    private static void OrderCell(IContainer c, string? text, float heightMm)
    {
        c.Border(Bw)
            .MinHeight(heightMm, Mm)
            .Padding(1.2f, Mm)
            .AlignTop()
            .Text(text ?? string.Empty)
            .Style(ThaiUrText.Base);
    }

    private static void SoapCell(IContainer c, Clinical05SoapSession? row, float heightMm)
    {
        c.Border(Bw)
            .MinHeight(heightMm, Mm)
            .Padding(1.4f, Mm)
            .AlignTop()
            .Column(col =>
            {
                col.Spacing(0.6f);
                Line(col, "S", row?.Subjective);
                col.Item().Text("O").Style(ThaiUrText.Bold);
                col.Item().Element(inner => ComposeObjective(inner, row));
                Line(col, "A", row?.Assessment);
                Line(col, "P", row?.Plan);
            });
    }

    private static void Line(ColumnDescriptor col, string label, string? value)
    {
        col.Item().Text(t =>
        {
            t.Span(label + "  ").Style(ThaiUrText.Bold);
            t.Span(value ?? string.Empty).Style(ThaiUrText.Base);
        });
    }

    private static void ComposeObjective(IContainer c, Clinical05SoapSession? row)
    {
        c.Column(col =>
        {
            col.Spacing(0.4f);
            col.Item().Text(t =>
            {
                t.Span("General Appearance  ").Style(ThaiUrText.Base);
                t.Span(Mark(row?.GeneralAppearance, GoodConscious) + " Good conscious  ").Style(ThaiUrText.Base);
                t.Span(Mark(row?.GeneralAppearance, Drowsiness) + " Drowsiness  ").Style(ThaiUrText.Base);
                t.Span(Mark(row?.GeneralAppearance, Other) + " Other ").Style(ThaiUrText.Base);
                t.Span(row?.GeneralAppearanceOther ?? string.Empty).Style(ThaiUrText.Base);
            });
            ExamLine(col, "HEENT", row?.Heent, row?.HeentNote);
            ExamLine(col, "Lung", row?.Lung, row?.LungNote);
            ExamLine(col, "Extremities", row?.Extremities, row?.ExtremitiesNote);
            col.Item().Text(t =>
            {
                t.Span("Other  ").Style(ThaiUrText.Base);
                t.Span(row?.ObjectiveOther ?? string.Empty).Style(ThaiUrText.Base);
            });
        });
    }

    private static void ExamLine(ColumnDescriptor col, string label, string? finding, string? note)
    {
        col.Item().Text(t =>
        {
            t.Span(label + "  ").Style(ThaiUrText.Base);
            t.Span(Mark(finding, Normal) + " N  ").Style(ThaiUrText.Base);
            t.Span(Mark(finding, Abnormal) + " Abn  ").Style(ThaiUrText.Base);
            t.Span(note ?? string.Empty).Style(ThaiUrText.Base);
        });
    }

    private static string Mark(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.Ordinal) ? "[x]" : "[ ]";
}
