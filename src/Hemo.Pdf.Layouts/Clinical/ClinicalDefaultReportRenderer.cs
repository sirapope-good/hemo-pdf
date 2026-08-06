using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Layouts.Clinical;

public sealed class ClinicalDefaultReportRenderer : Base.BaseReportRenderer
{
    public ClinicalDefaultReportRenderer(
        ClinicalDefaultDataProvider dataProvider,
        ClinicalDefaultComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
