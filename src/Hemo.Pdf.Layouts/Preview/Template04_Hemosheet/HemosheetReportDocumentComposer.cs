using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;

public sealed class HemosheetReportDocumentComposer : BaseReportDocumentComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;
    private readonly HemosheetSectionRendererRegistry _renderers;

    public HemosheetReportDocumentComposer(
        IHemosheetLayoutPlanner planner,
        HemosheetSectionRendererRegistry renderers)
    {
        _planner = planner;
        _renderers = renderers;
    }

    public override ReportDocument Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (HemosheetReportViewModel)dataModel;
        var blocks = ComposeContentBlocks(viewModel, context);

        return new ReportDocument
        {
            Meta = new ReportDocumentMeta
            {
                TemplateId = context.ReportTemplateId,
                Title = context.Metadata.Title,
                PageSize = "A4",
                GeneratedAt = DateTime.UtcNow.ToString("o"),
            },
            Branding = HemosheetBrandingPreviewMapper.Map(viewModel, context),
            Header = HemosheetHeaderPreviewMapper.Map(viewModel, context),
            Pages = [new ReportPage { Blocks = blocks }],
            Footer = HemosheetFooterPreviewMapper.Map(viewModel, context),
        };
    }

    protected override IReadOnlyList<ReportBlock> ComposeContentBlocks(
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var blocks = new List<ReportBlock>();

        foreach (var plan in _planner.Plan(viewModel, context.LayoutPackage))
        {
            blocks.AddRange(_renderers.MapToPreview(plan, viewModel, context));
        }

        return blocks;
    }
}
