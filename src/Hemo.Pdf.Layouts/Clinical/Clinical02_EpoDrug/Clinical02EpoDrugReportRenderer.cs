using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;

public sealed class Clinical02EpoDrugReportRenderer : BaseReportRenderer
{
    public Clinical02EpoDrugReportRenderer(
        Clinical02EpoDrugDataProvider dataProvider,
        Clinical02EpoDrugComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
