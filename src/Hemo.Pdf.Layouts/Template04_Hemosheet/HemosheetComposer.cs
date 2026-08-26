using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.Default;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Default;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetComposer : BaseReportComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;
    private readonly HemosheetSectionRendererRegistry _renderers;
    private readonly IHprpTemplateStore? _templates;
    private readonly ITenantContextAccessor? _tenant;
    private readonly ThaiUrHemosheetForm _thaiUrForm = new();
    private readonly DefaultHemosheetForm _defaultForm = new();

    public HemosheetComposer(
        IHemosheetLayoutPlanner planner,
        HemosheetSectionRendererRegistry renderers,
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver,
        IHprpTemplateStore? templates = null,
        ITenantContextAccessor? tenant = null)
        : base(headerResolver, footerResolver)
    {
        _planner = planner;
        _renderers = renderers;
        _templates = templates;
        _tenant = tenant;
    }

    public override object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (HemosheetReportViewModel)dataModel;
        var variant = HprpTemplatePaths.FromLayoutProfile(viewModel.LayoutContext.LayoutProfile);
        var package = context.LayoutPackage
            ?? _templates?.TryGetCached(
                _tenant?.TenantCode ?? context.TenantCode,
                ClinicalReportCatalog.HemodialysisRecord,
                variant);
        var kind = ClinicalReportLayoutResolver.Resolve(
            ClinicalReportCatalog.HemodialysisRecord,
            viewModel.LayoutContext.LayoutProfile,
            package?.Manifest);

        // ThaiUr → purple dense form; Default → CICM dense form; Rama → block planner.
        if (kind is ClinicalLayoutKind.ThaiUrForm or ClinicalLayoutKind.DefaultForm)
        {
            var margin = kind == ClinicalLayoutKind.ThaiUrForm
                ? HemosheetThaiUrStyle.PageMarginMm
                : HemosheetDefaultStyle.PageMarginMm;

            return new QuestLayout
            {
                MarginMillimeters = margin,
                MarginTop = margin,
                MarginBottom = margin,
                MarginLeft = margin,
                MarginRight = margin,
                Header = null,
                Content = c =>
                {
                    if (kind == ClinicalLayoutKind.ThaiUrForm)
                        _thaiUrForm.Compose(c, viewModel, context, package);
                    else
                        _defaultForm.Compose(c, viewModel, context, package);
                },
                Footer = null,
            };
        }

        return base.Compose(dataModel, context);
    }

    protected override void ComposeContent(
        IContainer container,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(PdfSectionMetrics.BlockSpacing);

            foreach (var plan in _planner.Plan(viewModel, context.LayoutPackage))
            {
                col.Item().Element(c => _renderers.ComposePdf(c, plan, viewModel, context));
            }
        });
    }
}
