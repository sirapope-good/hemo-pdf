using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Pixel composers for clinical-05 checklist dense widgets
/// (patient KV, year/month grid, text notes). Shared by composition + designer hosts.
/// </summary>
public static class Clinical05ChecklistSections
{
    private const Unit Mm = Unit.Millimetre;
    private const float FontSize = 9f;
    private const float HeaderFontSize = 10f;
    private const float SectionTitleFontSize = 11f;
    private const float CellPaddingMm = 1.5f;

    public static void ComposePatientTable(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        var p = vm.Patient;
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().Element(Cell).Text(t => LabelValue(t, "Patient name:", p.Name));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "DOB:", p.BirthDateLabel));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "HN:", p.HospitalNumber));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "Sessions per week:", p.SessionsPerWeekLabel));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "Dialysis days:", p.DialysisDays));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "Coverage scheme:", p.CoverageScheme));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "Dialysis mode:", p.DialysisMode));
            table.Cell().Element(Cell).Text(t => LabelValue(t, "Underlying:", p.Underlying));
        });
    }

    public static void ComposeChecklistGridSection(
        IContainer container,
        Clinical05ProgressNoteChecklistReportViewModel vm,
        HprpChrome? chrome = null)
    {
        var titleSize = chrome?.FontSize is > 0 and < 48
            ? chrome.FontSize.Value + 2f
            : SectionTitleFontSize;
        container.Column(col =>
        {
            col.Item().Text("Check lists").FontSize(titleSize).Bold();
            col.Item().PaddingTop(1, Mm).Element(c => ComposeChecklistGrid(c, vm, chrome));
        });
    }

    public static void ComposeChecklistGrid(
        IContainer container,
        Clinical05ProgressNoteChecklistReportViewModel vm,
        HprpChrome? chrome = null)
    {
        var monthCount = vm.Columns.Count;
        if (monthCount == 0 || vm.ChecklistItems.Count == 0)
        {
            container.Text("No progress note data available for the selected range.")
                .FontSize(ResolveFontSize(chrome));
            return;
        }

        var plan = HprpMatrixColumnPlan.Resolve(chrome?.ColumnWidths, monthCount);
        var fontSize = ResolveFontSize(chrome);

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var col in plan)
                {
                    if (col.ConstantMm)
                        columns.ConstantColumn(col.Value, Mm);
                    else
                        columns.RelativeColumn(col.Value);
                }
            });

            table.Cell().Element(c => HeaderCell(c, fontSize)).Text(string.Empty);
            foreach (var span in vm.YearSpans)
            {
                if (span.ColSpan <= 1)
                    table.Cell().Element(c => HeaderCell(c, fontSize)).AlignCenter().Text(span.Year.ToString());
                else
                    table.Cell().ColumnSpan((uint)span.ColSpan).Element(c => HeaderCell(c, fontSize)).AlignCenter()
                        .Text(span.Year.ToString());
            }

            table.Cell().Element(c => HeaderCell(c, fontSize)).Text("Item");
            foreach (var column in vm.Columns)
                table.Cell().Element(c => HeaderCell(c, fontSize)).AlignCenter().Text(column.CalendarMonth.ToString());

            string? lastGroup = null;
            foreach (var item in vm.ChecklistItems)
            {
                if (!string.IsNullOrWhiteSpace(item.Group) && item.Group != lastGroup)
                {
                    lastGroup = item.Group;
                    table.Cell().ColumnSpan((uint)(monthCount + 1)).Element(c => GroupCell(c, fontSize)).Text(item.Group!);
                }

                table.Cell().Element(c => BodyCell(c, fontSize)).Text(item.Label);
                foreach (var mark in item.Marks)
                    table.Cell().Element(c => BodyCell(c, fontSize)).AlignCenter().Text(mark ?? string.Empty);
            }
        });
    }

    /// <param name="allowPageBreak">
    /// Composition mode may break before notes; designer absolute boxes must not emit QuestPDF page breaks.
    /// </param>
    public static void ComposeTextNotes(
        IContainer container,
        Clinical05ProgressNoteChecklistReportViewModel vm,
        bool allowPageBreak = true)
    {
        if (vm.TextNotes.Count == 0)
        {
            container.Height(0.1f, Mm);
            return;
        }

        container.Column(col =>
        {
            if (allowPageBreak)
                col.Item().PageBreak();

            col.Item().Text("Text note").FontSize(SectionTitleFontSize).Bold();
            foreach (var note in vm.TextNotes)
            {
                col.Item().PaddingTop(2, Mm).Text(note.MonthLabel).FontSize(HeaderFontSize).Bold();
                col.Item().PaddingTop(1, Mm).Text(note.Content).FontSize(FontSize);
            }
        });
    }

    private static float ResolveFontSize(HprpChrome? chrome) =>
        chrome?.FontSize is > 0 and < 48 ? chrome.FontSize.Value : FontSize;

    private static void LabelValue(TextDescriptor text, string label, string value)
    {
        text.Span(label).Bold().FontSize(FontSize);
        text.Span(value).FontSize(FontSize);
    }

    private static IContainer Cell(IContainer container) =>
        container
            .Border(0.5f)
            .BorderColor(Colors.Black)
            .Padding(CellPaddingMm, Mm)
            .DefaultTextStyle(x => x.FontSize(FontSize));

    private static IContainer HeaderCell(IContainer container, float fontSize) =>
        container
            .Border(0.5f)
            .BorderColor(Colors.Black)
            .Padding(CellPaddingMm, Mm)
            .DefaultTextStyle(x => x.FontSize(fontSize).Bold());

    private static IContainer BodyCell(IContainer container, float fontSize) =>
        container
            .Border(0.5f)
            .BorderColor(Colors.Black)
            .Padding(CellPaddingMm, Mm)
            .DefaultTextStyle(x => x.FontSize(fontSize));

    private static IContainer GroupCell(IContainer container, float fontSize) =>
        container
            .Border(0.5f)
            .BorderColor(Colors.Black)
            .Padding(CellPaddingMm, Mm)
            .DefaultTextStyle(x => x.FontSize(fontSize).Bold());
}
