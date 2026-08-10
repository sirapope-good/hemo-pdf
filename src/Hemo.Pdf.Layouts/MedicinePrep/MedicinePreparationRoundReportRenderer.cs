using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.MedicinePrep;

public sealed class MedicinePreparationRoundReportRenderer : BaseReportRenderer
{
    public MedicinePreparationRoundReportRenderer(
        MedicinePreparationRoundDataProvider dataProvider,
        MedicinePreparationRoundComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
