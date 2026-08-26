using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Landscape Doctor progress note report — monthly checklist grid (clinical-05-checklist).
/// Widget order from <c>layout.body</c>; matches Doctor View pdfmake layout.
/// </summary>
public sealed class Clinical05ProgressNoteChecklistComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float MarginMm = 14f;
    private const float LabelColumnMm = 46f;
    private const float FontSize = 9f;
    private const float HeaderFontSize = 10f;
    private const float TitleFontSize = 14f;
    private const float SectionTitleFontSize = 11f;
    private const float CellPaddingMm = 1.5f;
    private const float SectionSpacingMm = 3f;

    private readonly IHprpTemplateStore? _templates;

    public Clinical05ProgressNoteChecklistComposer(IHprpTemplateStore? templates = null)
    {
        _templates = templates;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (Clinical05ProgressNoteChecklistReportViewModel)dataModel;
        var labels = HprpLabelResolver.Resolve(_templates, context);
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);
        var bodyNodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical05ChecklistBodyDefault,
            HprpClinicalWidgetSets.Clinical05ChecklistBodyAllowed,
            includeHeader: false);

        return new QuestLayout
        {
            Landscape = true,
            MarginMillimeters = MarginMm,
            MarginTop = 12f,
            MarginBottom = 10f,
            MarginLeft = MarginMm,
            MarginRight = MarginMm,
            Header = c => ComposePageHeader(c, vm),
            Content = c => ComposeBody(c, vm, labels, bodyNodes, context),
            Footer = ComposeFooter,
        };
    }

    private void ComposeBody(
        IContainer container,
        Clinical05ProgressNoteChecklistReportViewModel vm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> bodyNodes,
        PdfReportContext context)
    {
        var handlers = new Dictionary<string, Action<IContainer, HprpLayoutNode>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ClinicalChecklistPatient] = (c, _) => ComposePatientTable(c, vm),
            [HprpWidgetIds.ClinicalChecklistGrid] = (c, _) => ComposeChecklistGridSection(c, vm),
            [HprpWidgetIds.ClinicalChecklistTextNotes] = (c, _) => ComposeTextNotes(c, vm),
        };

        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm, Mm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                bodyNodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    private static void ComposePageHeader(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem();
                row.AutoItem().Text(vm.ReportCode).FontSize(HeaderFontSize);
            });

            col.Item().PaddingTop(2, Mm).AlignCenter().Text(vm.Title)
                .FontSize(TitleFontSize)
                .Bold();

            if (!string.IsNullOrWhiteSpace(vm.RangeLabel))
            {
                col.Item().PaddingTop(1, Mm).AlignCenter().Text(vm.RangeLabel)
                    .FontSize(HeaderFontSize);
            }
        });
    }

    private static void ComposePatientTable(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
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

    private static void ComposeChecklistGridSection(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        container.Column(col =>
        {
            col.Item().Text("Check lists").FontSize(SectionTitleFontSize).Bold();
            col.Item().PaddingTop(1, Mm).Element(c => ComposeChecklistGrid(c, vm));
        });
    }

    private static void ComposeChecklistGrid(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
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
                {
                    columns.RelativeColumn();
                }
            });

            table.Cell().Element(HeaderCell).Text(string.Empty);
            foreach (var span in vm.YearSpans)
            {
                if (span.ColSpan <= 1)
                {
                    table.Cell().Element(HeaderCell).AlignCenter().Text(span.Year.ToString());
                }
                else
                {
                    table.Cell().ColumnSpan((uint)span.ColSpan).Element(HeaderCell).AlignCenter().Text(span.Year.ToString());
                }
            }

            table.Cell().Element(HeaderCell).Text("Item");
            foreach (var column in vm.Columns)
            {
                table.Cell().Element(HeaderCell).AlignCenter().Text(column.CalendarMonth.ToString());
            }

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
                {
                    table.Cell().Element(BodyCell).AlignCenter().Text(mark ?? string.Empty);
                }
            }
        });
    }

    private static void ComposeTextNotes(IContainer container, Clinical05ProgressNoteChecklistReportViewModel vm)
    {
        if (vm.TextNotes.Count == 0)
        {
            container.Height(0.1f, Mm);
            return;
        }

        container.Column(col =>
        {
            col.Item().PageBreak();
            col.Item().Text("Text note").FontSize(SectionTitleFontSize).Bold();
            foreach (var note in vm.TextNotes)
            {
                col.Item().PaddingTop(2, Mm).Text(note.MonthLabel).FontSize(HeaderFontSize).Bold();
                col.Item().PaddingTop(1, Mm).Text(note.Content).FontSize(FontSize);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
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
