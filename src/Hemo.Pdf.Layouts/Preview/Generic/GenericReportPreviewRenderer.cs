using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Layouts.Generic;
using Hemo.Pdf.Layouts.Preview.Base;

namespace Hemo.Pdf.Layouts.Preview.Generic;

public sealed class GenericReportPreviewRenderer : BaseReportPreviewRenderer
{
    public GenericReportPreviewRenderer(
        GenericTemplateDataProvider dataProvider,
        GenericReportDocumentComposer composer)
        : base(dataProvider, composer)
    {
    }
}
