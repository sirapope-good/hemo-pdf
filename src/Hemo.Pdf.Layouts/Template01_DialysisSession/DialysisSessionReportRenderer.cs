using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Layouts.Template01_DialysisSession;

public sealed class DialysisSessionReportRenderer : Base.BaseReportRenderer
{
    public DialysisSessionReportRenderer(
        DialysisSessionDataProvider dataProvider,
        DialysisSessionComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
