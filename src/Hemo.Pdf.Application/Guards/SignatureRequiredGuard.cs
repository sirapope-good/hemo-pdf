using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application.Guards;

public sealed class SignatureRequiredGuard : IPdfGenerationGuard
{
    private readonly ISignatureStore _signatureStore;

    public SignatureRequiredGuard(ISignatureStore signatureStore)
    {
        _signatureStore = signatureStore;
    }

    public async Task EnsureCanGenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        if (!ReportTemplates.RequiresSignature(request.ReportTemplateId))
            return;

        var signatures = request.Signatures;
        if (signatures is null && !string.IsNullOrWhiteSpace(request.EntityId))
        {
            signatures = await _signatureStore.GetAsync(
                request.ReportTemplateId,
                request.EntityId,
                request.TenantCode,
                cancellationToken);
        }

        if (signatures?.IsFullySigned != true)
        {
            throw new PdfGenerationForbiddenException(
                $"Report template '{request.ReportTemplateId}' requires a fully signed document.");
        }
    }
}
