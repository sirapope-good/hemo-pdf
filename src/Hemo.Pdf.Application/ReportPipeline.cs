using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

internal static class ReportPipeline
{
    public static async Task<PdfReportContext> BuildContextAsync(
        GeneratePdfRequest request,
        IBrandingResolver brandingResolver,
        IReportSignatureResolver signatureResolver,
        CancellationToken cancellationToken,
        IHprpTemplateStore? templates = null,
        HprpPackage? layoutPackage = null)
    {
        var branding = await brandingResolver.ResolveAsync(request.TenantCode, cancellationToken);
        var signatures = await signatureResolver.ResolveAsync(request, cancellationToken);
        var templateDefinition = ResolveDefinition(request.ReportTemplateId, request.TenantCode, templates);

        return new PdfReportContext
        {
            ReportTemplateId = request.ReportTemplateId,
            TenantCode = request.TenantCode,
            EntityId = request.EntityId,
            Branding = branding,
            Data = request.Data,
            Signatures = signatures,
            Parameters = request.Parameters ?? new Dictionary<string, object?>(),
            Metadata = BuildMetadata(request, branding, templateDefinition),
            LayoutPackage = layoutPackage,
        };
    }

    private static ReportTemplateDefinition? ResolveDefinition(
        string reportTemplateId,
        string tenantCode,
        IHprpTemplateStore? templates)
    {
        if (HprpCatalog.TryGetDefinition(templates, tenantCode, reportTemplateId, out var fromPackage)
            && fromPackage is not null)
        {
            return fromPackage;
        }

        ClinicalReportCatalog.TryGetDefinition(reportTemplateId, out var clinical);
        return clinical;
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
