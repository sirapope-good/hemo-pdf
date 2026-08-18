using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

public sealed class Clinical05ProgressNoteReportRenderer : BaseReportRenderer
{
    public Clinical05ProgressNoteReportRenderer(
        Clinical05ProgressNoteDataProvider dataProvider,
        Clinical05ProgressNoteComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
