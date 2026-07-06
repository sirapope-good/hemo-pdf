using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Application;

public sealed class ReportPreviewService : IReportPreviewService
{
    private readonly IBrandingResolver _brandingResolver;
    private readonly IPdfGenerationGuard _guard;
    private readonly ISignatureStore _signatureStore;
    private readonly IReportPreviewRendererFactory _rendererFactory;

    public ReportPreviewService(
        IBrandingResolver brandingResolver,
        IPdfGenerationGuard guard,
        ISignatureStore signatureStore,
        IReportPreviewRendererFactory rendererFactory)
    {
        _brandingResolver = brandingResolver;
        _guard = guard;
        _signatureStore = signatureStore;
        _rendererFactory = rendererFactory;
    }

    public async Task<ReportDocument> PreviewAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanGenerateAsync(request, cancellationToken);

        var branding = await _brandingResolver.ResolveAsync(request.TenantCode, cancellationToken);
        var signatures = await ResolveSignaturesAsync(request, cancellationToken);
        ReportTemplates.TryGetDefinition(request.ReportTemplateId, out var templateDefinition);

        var context = new PdfReportContext
        {
            ReportTemplateId = request.ReportTemplateId,
            TenantCode = request.TenantCode,
            EntityId = request.EntityId,
            Branding = branding,
            Data = request.Data,
            Signatures = signatures,
            Parameters = request.Parameters ?? new Dictionary<string, object?>(),
            Metadata = BuildMetadata(request, branding, templateDefinition),
        };

        var renderer = _rendererFactory.Create(request.ReportTemplateId);
        return await renderer.RenderPreviewAsync(context, cancellationToken);
    }

    private async Task<ReportSignatureContext?> ResolveSignaturesAsync(
        GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Signatures is not null)
            return request.Signatures;

        if (string.IsNullOrWhiteSpace(request.EntityId))
            return null;

        return await _signatureStore.GetAsync(
            request.ReportTemplateId,
            request.EntityId,
            request.TenantCode,
            cancellationToken);
    }

    private static ReportMetadata BuildMetadata(
        GeneratePdfRequest request,
        CustomerBrandingProfile branding,
        ReportTemplateDefinition? templateDefinition)
    {
        var title = templateDefinition?.DisplayName ?? request.ReportTemplateId;
        string? reportCode = null;

        if (!string.IsNullOrWhiteSpace(branding.Header.ReportCodePrefix))
        {
            var suffix = request.EntityId ?? Guid.NewGuid().ToString("N")[..8];
            reportCode = $"{branding.Header.ReportCodePrefix}-{suffix}";
        }

        return new ReportMetadata
        {
            Title = title,
            ReportCode = reportCode,
        };
    }
}
