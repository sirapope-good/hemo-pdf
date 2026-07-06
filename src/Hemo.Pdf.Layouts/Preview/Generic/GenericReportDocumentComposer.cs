using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Preview.Generic;

public sealed class GenericReportDocumentComposer : BaseReportDocumentComposer<SimpleReportViewModel>
{
    protected override IReadOnlyList<ReportBlock> ComposeContentBlocks(
        SimpleReportViewModel viewModel,
        PdfReportContext context)
    {
        var blocks = new List<ReportBlock>();

        var keyValue = KeyValueTablePreviewMapper.Map(viewModel);
        if (keyValue is not null)
        {
            blocks.Add(keyValue);
        }

        return blocks;
    }
}
