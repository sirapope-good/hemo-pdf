using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;

public sealed class HemosheetReportPreviewRenderer : BaseReportPreviewRenderer
{
    public HemosheetReportPreviewRenderer(
        HemosheetDataProvider dataProvider,
        HemosheetReportDocumentComposer composer)
        : base(dataProvider, composer)
    {
    }
}
