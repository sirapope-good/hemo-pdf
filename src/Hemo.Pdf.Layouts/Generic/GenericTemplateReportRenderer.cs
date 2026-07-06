using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Layouts.Generic;

public sealed class GenericTemplateReportRenderer : Base.BaseReportRenderer
{
    public GenericTemplateReportRenderer(
        GenericTemplateDataProvider dataProvider,
        GenericTemplateComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
