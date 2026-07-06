using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Layouts.Template01_DialysisSession;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Preview.Template01_DialysisSession;

public sealed class DialysisSessionReportDocumentComposer
    : BaseReportDocumentComposer<DialysisSessionViewModel>
{
    protected override IReadOnlyList<ReportBlock> ComposeContentBlocks(
        DialysisSessionViewModel viewModel,
        PdfReportContext context)
    {
        var blocks = new List<ReportBlock>();

        AddIfNotNull(blocks, PatientInfoPreviewMapper.Map(viewModel));
        AddIfNotNull(blocks, KeyValueTablePreviewMapper.Map(viewModel));
        AddIfNotNull(blocks, DataGridPreviewMapper.Map(viewModel));
        AddIfNotNull(blocks, SignaturePreviewMapper.Map(context));

        return blocks;
    }

    private static void AddIfNotNull(List<ReportBlock> blocks, ReportBlock? block)
    {
        if (block is not null)
        {
            blocks.Add(block);
        }
    }
}
