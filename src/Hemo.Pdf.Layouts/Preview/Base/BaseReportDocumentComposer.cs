using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Preview.Base;

public abstract class BaseReportDocumentComposer<TViewModel> : IReportDocumentComposer
{
    public virtual ReportDocument Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (TViewModel)dataModel;
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
            Branding = MapBranding(viewModel, context),
            Header = HeaderPreviewMapper.Map(context),
            Pages = [new ReportPage { Blocks = blocks }],
            Footer = FooterPreviewMapper.Map(context),
        };
    }

    protected virtual ReportBranding MapBranding(TViewModel viewModel, PdfReportContext context) =>
        BrandingPreviewMapper.Map(context);

    protected abstract IReadOnlyList<ReportBlock> ComposeContentBlocks(
        TViewModel viewModel,
        PdfReportContext context);
}
