using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical04_Prescription;

public sealed class Clinical04PrescriptionReportRenderer : BaseReportRenderer
{
    public Clinical04PrescriptionReportRenderer(
        Clinical04PrescriptionDataProvider dataProvider,
        Clinical04PrescriptionComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
