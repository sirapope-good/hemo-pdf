using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application.Guards;

public sealed class SignatureRequiredGuard : IPdfGenerationGuard
{
    private readonly IReportSignatureResolver _signatureResolver;

    public SignatureRequiredGuard(IReportSignatureResolver signatureResolver)
    {
        _signatureResolver = signatureResolver;
    }

    public async Task EnsureCanGenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        if (!ReportTemplates.RequiresSignature(request.ReportTemplateId))
            return;

        var signatures = await _signatureResolver.ResolveAsync(request, cancellationToken);
        if (signatures?.IsFullySigned != true)
        {
            throw new PdfGenerationForbiddenException(
                $"Report template '{request.ReportTemplateId}' requires a fully signed document.");
        }
    }
}
