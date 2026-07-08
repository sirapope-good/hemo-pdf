using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetComposer : BaseReportComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;
    private readonly HemosheetSectionRendererRegistry _renderers;
    private readonly ThaiUrHemosheetForm _thaiUrForm = new();

    public HemosheetComposer(
        IHemosheetLayoutPlanner planner,
        HemosheetSectionRendererRegistry renderers,
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
        _planner = planner;
        _renderers = renderers;
    }

    public override object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (HemosheetReportViewModel)dataModel;

        // The ThaiUR "Hemodialysis Record" is a dense, self-contained single-page form (own header
        // and footer), so it bypasses the block-flow planner and header/footer resolvers to match
        // the Telerik original pixel-for-pixel. Default/Rama keep the flexible block-flow path.
        if (viewModel.LayoutContext.LayoutProfile == HemosheetLayoutProfile.ThaiUr)
        {
            const float margin = HemosheetThaiUrStyle.PageMarginMm;
            return new QuestLayout
            {
                MarginMillimeters = margin,
                MarginTop = margin,
                MarginBottom = margin,
                MarginLeft = margin,
                MarginRight = margin,
                Header = null,
                Content = c => _thaiUrForm.Compose(c, viewModel, context),
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

            foreach (var plan in _planner.Plan(viewModel))
            {
                col.Item().Element(c => _renderers.ComposePdf(c, plan, viewModel, context));
            }
        });
    }
}
