using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetReportRenderer : Base.BaseReportRenderer
{
    public HemosheetReportRenderer(
        HemosheetDataProvider dataProvider,
        HemosheetComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
