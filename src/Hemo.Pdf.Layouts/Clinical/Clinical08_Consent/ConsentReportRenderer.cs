using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;

public sealed class ConsentReportRenderer : BaseReportRenderer
{
    public ConsentReportRenderer(
        ConsentReportDataProvider dataProvider,
        ConsentReportComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
