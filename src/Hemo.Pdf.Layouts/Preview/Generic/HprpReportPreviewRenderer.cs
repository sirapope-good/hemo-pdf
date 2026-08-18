using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Preview.Base;

namespace Hemo.Pdf.Layouts.Preview.Generic;

public sealed class HprpReportPreviewRenderer : BaseReportPreviewRenderer
{
    public HprpReportPreviewRenderer(
        ClinicalDefaultDataProvider dataProvider,
        HprpReportDocumentComposer composer)
        : base(dataProvider, composer)
    {
    }
}
