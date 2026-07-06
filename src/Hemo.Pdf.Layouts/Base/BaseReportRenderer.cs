using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;

namespace Hemo.Pdf.Layouts.Base;

public abstract class BaseReportRenderer : IReportRenderer
{
    private readonly IReportDataProvider _dataProvider;
    private readonly ILayoutComposer _composer;
    private readonly IPdfRenderer _pdfRenderer;

    protected BaseReportRenderer(
        IReportDataProvider dataProvider,
        ILayoutComposer composer,
        IPdfRenderer pdfRenderer)
    {
        _dataProvider = dataProvider;
        _composer = composer;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<byte[]> RenderReportAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        var model = await _dataProvider.GetDataAsync(context, cancellationToken);
        var layout = _composer.Compose(model, context);
        return await _pdfRenderer.RenderAsync(layout, cancellationToken);
    }
}
