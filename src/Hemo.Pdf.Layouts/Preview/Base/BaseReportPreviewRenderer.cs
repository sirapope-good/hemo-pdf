using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Layouts.Preview.Base;

public abstract class BaseReportPreviewRenderer : IReportPreviewRenderer
{
    private readonly IReportDataProvider _dataProvider;
    private readonly IReportDocumentComposer _composer;

    protected BaseReportPreviewRenderer(
        IReportDataProvider dataProvider,
        IReportDocumentComposer composer)
    {
        _dataProvider = dataProvider;
        _composer = composer;
    }

    public async Task<ReportDocument> RenderPreviewAsync(
        PdfReportContext context,
        CancellationToken cancellationToken)
    {
        var model = await _dataProvider.GetDataAsync(context, cancellationToken);
        return _composer.Compose(model, context);
    }
}
