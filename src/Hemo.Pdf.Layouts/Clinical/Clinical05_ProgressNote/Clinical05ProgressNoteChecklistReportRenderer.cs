using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Base;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

public sealed class Clinical05ProgressNoteChecklistReportRenderer : BaseReportRenderer
{
    public Clinical05ProgressNoteChecklistReportRenderer(
        Clinical05ProgressNoteChecklistDataProvider dataProvider,
        Clinical05ProgressNoteChecklistComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
