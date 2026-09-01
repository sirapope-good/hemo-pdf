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
    private const float LabelColumnMm = 46f;
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

    public static void ComposeChecklistGridSection(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        container.Column(col =>
        {
            col.Item().Text("Check lists").FontSize(SectionTitleFontSize).Bold();
            col.Item().PaddingTop(1, Mm).Element(c => ComposeChecklistGrid(c, vm));
        });
    }

    public static void ComposeChecklistGrid(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        var monthCount = vm.Columns.Count;
        if (monthCount == 0 || vm.ChecklistItems.Count == 0)
        {
            container.Text("No progress note data available for the selected range.").FontSize(FontSize);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(LabelColumnMm, Mm);
                for (var i = 0; i < monthCount; i++)
                    columns.RelativeColumn();
            });

            table.Cell().Element(HeaderCell).Text(string.Empty);
            foreach (var span in vm.YearSpans)
            {
                if (span.ColSpan <= 1)
                    table.Cell().Element(HeaderCell).AlignCenter().Text(span.Year.ToString());
                else
                    table.Cell().ColumnSpan((uint)span.ColSpan).Element(HeaderCell).AlignCenter().Text(span.Year.ToString());
            }

            table.Cell().Element(HeaderCell).Text("Item");
            foreach (var column in vm.Columns)
                table.Cell().Element(HeaderCell).AlignCenter().Text(column.CalendarMonth.ToString());

            string? lastGroup = null;
            foreach (var item in vm.ChecklistItems)
            {
                if (!string.IsNullOrWhiteSpace(item.Group) && item.Group != lastGroup)
                {
                    lastGroup = item.Group;
                    table.Cell().ColumnSpan((uint)(monthCount + 1)).Element(GroupCell).Text(item.Group!);
                }

                table.Cell().Element(BodyCell).Text(item.Label);
                foreach (var mark in item.Marks)
                    table.Cell().Element(BodyCell).AlignCenter().Text(mark ?? string.Empty);
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

    private static IContainer HeaderCell(IContainer container) =>
        Cell(container).DefaultTextStyle(x => x.FontSize(FontSize).Bold());

    private static IContainer BodyCell(IContainer container) => Cell(container);

    private static IContainer GroupCell(IContainer container) =>
        Cell(container).DefaultTextStyle(x => x.FontSize(FontSize).Bold());
}
