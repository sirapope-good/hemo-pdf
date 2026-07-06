using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Layouts.Template01_DialysisSession;

namespace Hemo.Pdf.Layouts.Preview.Template01_DialysisSession;

public sealed class DialysisSessionReportPreviewRenderer : BaseReportPreviewRenderer
{
    public DialysisSessionReportPreviewRenderer(
        DialysisSessionDataProvider dataProvider,
        DialysisSessionReportDocumentComposer composer)
        : base(dataProvider, composer)
    {
    }
}
