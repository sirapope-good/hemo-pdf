using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

public sealed class PdfGenerationService : IPdfGenerationService
{
    private readonly IBrandingResolver _brandingResolver;
    private readonly IPdfGenerationGuard _guard;
    private readonly IReportSignatureResolver _signatureResolver;
    private readonly IReportRendererFactory _rendererFactory;
    private readonly ITenantContextAccessor _tenantContext;

    public PdfGenerationService(
        IBrandingResolver brandingResolver,
        IPdfGenerationGuard guard,
        IReportSignatureResolver signatureResolver,
        IReportRendererFactory rendererFactory,
        ITenantContextAccessor tenantContext)
    {
        _brandingResolver = brandingResolver;
        _guard = guard;
        _signatureResolver = signatureResolver;
        _rendererFactory = rendererFactory;
        _tenantContext = tenantContext;
    }

    public async Task<byte[]> GenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        GeneratePdfRequestValidator.Validate(request, _tenantContext);
        await _guard.EnsureCanGenerateAsync(request, cancellationToken);

        var branding = await _brandingResolver.ResolveAsync(request.TenantCode, cancellationToken);
        var signatures = await _signatureResolver.ResolveAsync(request, cancellationToken);
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
        return await renderer.RenderReportAsync(context, cancellationToken);
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
