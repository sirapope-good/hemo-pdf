using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application.Guards;

public sealed class SignatureRequiredGuard : IPdfGenerationGuard
{
    private readonly IReportSignatureResolver _signatureResolver;
    private readonly IHprpTemplateStore? _templates;

    public SignatureRequiredGuard(
        IReportSignatureResolver signatureResolver,
        IHprpTemplateStore? templates = null)
    {
        _signatureResolver = signatureResolver;
        _templates = templates;
    }

    public async Task EnsureCanGenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        if (!RequiresSignature(request))
            return;

        // Empty template forms are never fully signed — allow generate/print for layout review.
        if (HemosheetFetchSpec.IsTemplateRequest(request))
            return;

        var signatures = await _signatureResolver.ResolveAsync(request, cancellationToken);
        if (signatures?.IsFullySigned != true)
        {
            throw new PdfGenerationForbiddenException(
                $"Report template '{request.ReportTemplateId}' requires a fully signed document.");
        }
    }

    private bool RequiresSignature(GeneratePdfRequest request)
    {
        if (HprpCatalog.TryGetDefinition(_templates, request.TenantCode, request.ReportTemplateId, out var fromPackage)
            && fromPackage is not null)
        {
            return fromPackage.RequiresSignature;
        }

        return ClinicalReportCatalog.RequiresSignature(request.ReportTemplateId);
    }
}
