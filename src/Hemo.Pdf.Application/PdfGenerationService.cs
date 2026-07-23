using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

public sealed class PdfGenerationService : IPdfGenerationService
{
    private readonly IBrandingResolver _brandingResolver;
    private readonly IPdfGenerationGuard _guard;
    private readonly IReportSignatureResolver _signatureResolver;
    private readonly IReportRendererFactory _rendererFactory;
    private readonly ReportRequestPipeline _requestPipeline;

    public PdfGenerationService(
        IBrandingResolver brandingResolver,
        IPdfGenerationGuard guard,
        IReportSignatureResolver signatureResolver,
        IReportRendererFactory rendererFactory,
        ReportRequestPipeline requestPipeline)
    {
        _brandingResolver = brandingResolver;
        _guard = guard;
        _signatureResolver = signatureResolver;
        _rendererFactory = rendererFactory;
        _requestPipeline = requestPipeline;
    }

    public async Task<byte[]> GenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        request = await _requestPipeline.PrepareAsync(request, cancellationToken);

        await _guard.EnsureCanGenerateAsync(request, cancellationToken);

        var context = await ReportPipeline.BuildContextAsync(
            request,
            _brandingResolver,
            _signatureResolver,
            cancellationToken);

        var renderer = _rendererFactory.Create(request.ReportTemplateId);
        return await renderer.RenderReportAsync(context, cancellationToken);
    }
}
