using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Designer;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Landscape Hemodialysis Progress note — monthly checklist grid (clinical-05-checklist).
/// Composition: widget order from <c>layout.body</c>. Designer packs use
/// <see cref="DesignerPageComposer"/> (box-text header + dense patient/grid/notes).
/// </summary>
public sealed class Clinical05ProgressNoteChecklistComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float MarginMm = 14f;
    private const float HeaderFontSize = 10f;
    private const float TitleFontSize = 14f;
    private const float SectionSpacingMm = 3f;

    private readonly IHprpTemplateStore? _templates;
    private readonly IHprpTablePresetCatalog? _presets;
    private readonly IHprpHeaderPresetCatalog? _headerPresets;

    public Clinical05ProgressNoteChecklistComposer(IHprpTemplateStore? templates = null)
        : this(templates, null, null)
    {
    }

    public Clinical05ProgressNoteChecklistComposer(
        IHprpTemplateStore? templates,
        IHprpTablePresetCatalog? presets,
        IHprpHeaderPresetCatalog? headerPresets)
    {
        _templates = templates;
        _presets = presets;
        _headerPresets = headerPresets;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (Clinical05ProgressNoteChecklistReportViewModel)dataModel;
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);

        if (package is not null && HprpLayoutModes.IsDesigner(package.Manifest))
        {
            JsonElement? data = context.Data is JsonElement je ? je : null;
            var designerVm = DesignerCanvasViewModel.FromPackage(
                package,
                data,
                HprpLabelResolver.Resolve(_templates, context),
                _presets?.LoadAll(),
                _headerPresets?.LoadAll(),
                boundModel: vm);
            return DesignerPageComposer.Compose(designerVm, context);
        }

        var labels = HprpLabelResolver.Resolve(_templates, context);
        var page = HprpPageLayout.FromPackage(
            package,
            new HprpPageFallback
            {
                Top = 12f,
                Bottom = 10f,
                Left = MarginMm,
                Right = MarginMm,
                SpacingMm = SectionSpacingMm,
            });
        var bodyNodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical05ChecklistBodyDefault,
            HprpClinicalWidgetSets.Clinical05ChecklistBodyAllowed,
            includeHeader: false);

        return HprpQuestPages.Create(
            page,
            header: c => ComposePageHeader(c, vm),
            content: c => ComposeBody(c, vm, labels, bodyNodes, context),
            footer: ComposeFooter,
            landscape: true);
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
            [HprpWidgetIds.ClinicalChecklistPatient] = (c, _) =>
                Clinical05ChecklistSections.ComposePatientTable(c, vm),
            [HprpWidgetIds.ClinicalChecklistGrid] = (c, _) =>
                Clinical05ChecklistSections.ComposeChecklistGridSection(c, vm),
            [HprpWidgetIds.ClinicalChecklistTextNotes] = (c, _) =>
                Clinical05ChecklistSections.ComposeTextNotes(c, vm),
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
}
