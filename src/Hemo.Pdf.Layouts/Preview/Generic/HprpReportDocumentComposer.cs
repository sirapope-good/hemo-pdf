using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Preview.Generic;

public sealed class HprpReportDocumentComposer : BaseReportDocumentComposer<HprpBoundViewModel>
{
    protected override ReportBranding MapBranding(HprpBoundViewModel viewModel, PdfReportContext context) =>
        BrandingPreviewMapper.Map(context, viewModel.SectionHeaderFill);

    protected override IReadOnlyList<ReportBlock> ComposeContentBlocks(
        HprpBoundViewModel viewModel,
        PdfReportContext context)
    {
        return viewModel.Blocks;
    }
}
