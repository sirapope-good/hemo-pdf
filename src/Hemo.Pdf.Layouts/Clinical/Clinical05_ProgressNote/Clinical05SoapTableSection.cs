using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
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

    private const float SoapLetterMm = 8f;
    private const float ExamLabelMm = 24f;
    private const float ExamNColMm = 16f;
    private const float ExamAbnColMm = 20f;
    private const float LineMinMm = 4.6f;
    private const float CheckSizePt = 6.5f;
    private const float CheckGapPt = 2f;

    public const string GoodConscious = "goodConscious";
    public const string Drowsiness = "drowsiness";
    public const string Other = "other";
    public const string Normal = "normal";
    public const string Abnormal = "abnormal";

    public void Compose(
        IContainer container,
        Clinical05ProgressNoteReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string>? labels = null)
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

            t.Header(header =>
            {
                HeaderCell(header, HprpLabels.Get(labels, "colDate", "DATE"));
                HeaderCell(header, HprpLabels.Get(labels, "colProgress", "PROGRESS NOTE"));
                HeaderCell(header, HprpLabels.Get(labels, "colOrderOneDay", "ORDER FOR ONE DAY"));
                HeaderCell(header, HprpLabels.Get(labels, "colOrderContinuation", "ORDER FOR CONTINUATION"));
            });

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

    private static void HeaderCell(TableCellDescriptor t, string text)
    {
        t.Cell()
            .Border(Bw)
            .Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
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
            .Padding(1.6f, Mm)
            .AlignTop()
            .Column(col =>
            {
                col.Spacing(1.1f);
                SoapLine(col, "S", row?.Subjective);
                col.Item().Element(inner => ComposeObjective(inner, row));
                SoapLine(col, "A", row?.Assessment);
                SoapLine(col, "P", row?.Plan);
            });
    }

    private static void SoapLine(ColumnDescriptor col, string letter, string? value)
    {
        col.Item().MinHeight(LineMinMm, Mm).Row(r =>
        {
            r.ConstantItem(SoapLetterMm, Mm).AlignTop().Text(letter + " :").Style(ThaiUrText.Bold);
            r.RelativeItem().AlignTop().Text(value ?? string.Empty).Style(ThaiUrText.Base);
        });
    }

    private static void ComposeObjective(IContainer c, Clinical05SoapSession? row)
    {
        c.Column(col =>
        {
            col.Spacing(0.7f);

            col.Item().MinHeight(LineMinMm, Mm).Row(r =>
            {
                r.ConstantItem(SoapLetterMm, Mm).AlignMiddle().Text("O :").Style(ThaiUrText.Bold);
                r.RelativeItem().AlignMiddle().Text("General Appearance :").Style(ThaiUrText.Base);
            });

            col.Item().PaddingLeft(SoapLetterMm, Mm).Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.35f);
                    cols.RelativeColumn(1.1f);
                    cols.RelativeColumn(1.55f);
                });
                t.Cell().Element(cell => CheckOption(cell, Is(row?.GeneralAppearance, GoodConscious), "Good conscious"));
                t.Cell().Element(cell => CheckOption(cell, Is(row?.GeneralAppearance, Drowsiness), "Drowsiness"));
                t.Cell().Element(cell => CheckOption(
                    cell,
                    Is(row?.GeneralAppearance, Other),
                    "Other",
                    row?.GeneralAppearanceOther));
            });

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(ExamLabelMm, Mm);
                    cols.ConstantColumn(ExamNColMm, Mm);
                    cols.ConstantColumn(ExamAbnColMm, Mm);
                    cols.RelativeColumn();
                });
                ExamRow(t, "HEENT", row?.Heent, row?.HeentNote);
                ExamRow(t, "Lung", row?.Lung, row?.LungNote);
                ExamRow(t, "Extremities", row?.Extremities, row?.ExtremitiesNote);
            });

            col.Item().MinHeight(LineMinMm, Mm).Row(r =>
            {
                r.ConstantItem(ExamLabelMm, Mm).AlignTop().Text("Other :").Style(ThaiUrText.Base);
                r.RelativeItem().AlignTop().Text(row?.ObjectiveOther ?? string.Empty).Style(ThaiUrText.Base);
            });
        });
    }

    private static void ExamRow(TableDescriptor t, string label, string? finding, string? note)
    {
        t.Cell().MinHeight(LineMinMm, Mm).AlignMiddle().Text(label + " :").Style(ThaiUrText.Base);
        t.Cell().MinHeight(LineMinMm, Mm).Element(c => CheckOption(c, Is(finding, Normal), "N"));
        t.Cell().MinHeight(LineMinMm, Mm).Element(c => CheckOption(c, Is(finding, Abnormal), "Abn"));
        t.Cell().MinHeight(LineMinMm, Mm).AlignMiddle().PaddingLeft(1.2f, Mm)
            .Text(note ?? string.Empty)
            .Style(ThaiUrText.Base);
    }

    private static void CheckOption(IContainer c, bool isChecked, string label, string? extra = null)
    {
        c.AlignMiddle().Row(r =>
        {
            r.Checkbox(isChecked, CheckSizePt);
            r.ConstantItem(CheckGapPt);
            r.RelativeItem().AlignMiddle().Text(t =>
            {
                t.Span(label).Style(ThaiUrText.Base);
                if (!string.IsNullOrWhiteSpace(extra))
                    t.Span("  " + extra).Style(ThaiUrText.Base);
            });
        });
    }

    private static bool Is(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.Ordinal);
}
