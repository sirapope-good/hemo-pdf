using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

public sealed class Clinical01HctEpoReportRenderer : BaseReportRenderer
{
    public Clinical01HctEpoReportRenderer(
        Clinical01HctEpoDataProvider dataProvider,
        Clinical01HctEpoComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
