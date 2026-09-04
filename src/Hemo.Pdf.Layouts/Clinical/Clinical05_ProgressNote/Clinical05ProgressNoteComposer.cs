using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Designer;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Dense QuestPDF layout for clinical-05: repeating header from <c>layout.header</c>,
/// body widgets from <c>layout.body</c> (SOAP table). Designer packs use
/// <see cref="DesignerPageComposer"/> (header preset + dense soap-table).
/// </summary>
public sealed class Clinical05ProgressNoteComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float A4HeightMm = 297f;
    private const float PageNumberFooterMm = 7f;
    private const float LayoutSafetyMm = 1.5f;
    private const float SectionSpacingMm = 2f;
    private const float MinBlockHeightMm = 90f;

    private readonly Clinical05SoapTableSection _table = new();
    private readonly IHprpTemplateStore? _templates;
    private readonly IHprpTablePresetCatalog? _presets;
    private readonly IHprpHeaderPresetCatalog? _headerPresets;

    public Clinical05ProgressNoteComposer(IHprpTemplateStore? templates = null)
        : this(templates, null, null)
    {
    }

    public Clinical05ProgressNoteComposer(
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
        var vm = (Clinical05ProgressNoteReportViewModel)dataModel;
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

        var page = HprpPageLayout.FromPackage(
            package,
            HprpPageFallback.Uniform(HemosheetThaiUrStyle.PageMarginMm, SectionSpacingMm));
        var bodyNodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical05BodyDefault,
            HprpClinicalWidgetSets.Clinical05BodyAllowed,
            includeHeader: false);
        var soapChrome = bodyNodes
            .FirstOrDefault(n => string.Equals(n.Widget, HprpWidgetIds.ClinicalSoapTable, StringComparison.OrdinalIgnoreCase))
            ?.Chrome;
        var tableHeaderMm = HprpChrome.ResolveHeaderHeightMm(soapChrome, HemosheetThaiUrStyle.HeaderBarHeightMm);
        var rowHeightMm = BudgetRowHeightMm(vm, page.Vertical, tableHeaderMm, page.SpacingMm);

        var labels = HprpLabelResolver.Resolve(_templates, context);
        var headerWidget = HprpLayoutPlan.ResolveHeaderWidget(
            package,
            HprpWidgetIds.ThaiUrHeader,
            HprpClinicalWidgetSets.Clinical05HeaderAllowed);

        return HprpQuestPages.Create(
            page,
            header: string.Equals(headerWidget, HprpWidgetIds.ThaiUrHeader, StringComparison.OrdinalIgnoreCase)
                ? c => ComposeRepeatingHeader(c, vm, page.SpacingMm)
                : null,
            content: c => ComposeBody(c, vm, rowHeightMm, labels, bodyNodes, context, page.SpacingMm),
            footer: null);
    }

    private void ComposeBody(
        IContainer container,
        Clinical05ProgressNoteReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> bodyNodes,
        PdfReportContext context,
        float spacingMm)
    {
        var handlers = new Dictionary<string, Action<IContainer, HprpLayoutNode>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ClinicalSoapTable] = (c, node) =>
                _table.Compose(c, vm, rowHeightMm, labels, node, context),
        };

        container.Column(col =>
        {
            col.Spacing(spacingMm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                bodyNodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    private static void ComposeRepeatingHeader(
        IContainer container,
        Clinical05ProgressNoteReportViewModel vm,
        float spacingMm)
    {
        // Gap under thaiur.header ↔ first body block — driven by layout.page.spacingMm (Studio Page inspector).
        var gap = spacingMm >= 0 ? spacingMm : SectionSpacingMm;
        var box = gap > 0 ? container.PaddingBottom(gap, Mm) : container;
        box.Element(c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));
    }

    /// <summary>
    /// Page budget for SOAP mode: ~2 progress-note rows per A4 (1 plan page).
    /// Row height is fixed — SOAP overflow must not grow the table.
    /// </summary>
    internal static float BudgetRowHeightMm(
        Clinical05ProgressNoteReportViewModel vm,
        float verticalMarginMm = -1,
        float tableHeaderMm = -1,
        float sectionSpacingMm = -1)
    {
        var margin = verticalMarginMm >= 0 ? verticalMarginMm : 2f * HemosheetThaiUrStyle.PageMarginMm;
        var pageContentMm = A4HeightMm
            - margin
            - PageNumberFooterMm;

        var headerMm = HemosheetThaiUrStyle.TitleHeightMm
            + HemosheetThaiUrStyle.MetaRowHeightMm;
        var soapHeaderMm = tableHeaderMm > 0 ? tableHeaderMm : HemosheetThaiUrStyle.HeaderBarHeightMm;
        var gapMm = sectionSpacingMm >= 0 ? sectionSpacingMm : SectionSpacingMm;
        var availableForRowsMm = pageContentMm
            - headerMm
            - gapMm
            - soapHeaderMm
            - LayoutSafetyMm;
        var fromBudget = availableForRowsMm / Clinical05SoapTableSection.MinEmptyRows;
        // Never request more row height than leftover page (tall headerHeightMm used to overflow).
        return fromBudget >= MinBlockHeightMm ? fromBudget : Math.Max(fromBudget, 8f);
    }
}
