using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Layouts.Placeholder;

public sealed class PlaceholderReportRenderer : Base.BaseReportRenderer
{
    public PlaceholderReportRenderer(
        PlaceholderDataProvider dataProvider,
        PlaceholderComposer composer,
        IPdfRenderer pdfRenderer)
        : base(dataProvider, composer, pdfRenderer)
    {
    }
}
