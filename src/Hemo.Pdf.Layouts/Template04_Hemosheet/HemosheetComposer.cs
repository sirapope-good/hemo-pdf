using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetComposer : BaseReportComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;
    private readonly HemosheetSectionRendererRegistry _renderers;

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
