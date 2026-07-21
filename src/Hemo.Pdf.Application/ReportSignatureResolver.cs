using Hemo.Pdf.Application.Mock;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

public interface IReportSignatureResolver
{
    Task<ReportSignatureContext?> ResolveAsync(
        GeneratePdfRequest request,
        CancellationToken cancellationToken);
}

public sealed class ReportSignatureResolver : IReportSignatureResolver
{
    private readonly ISignatureStore _signatureStore;

    public ReportSignatureResolver(ISignatureStore signatureStore)
    {
        _signatureStore = signatureStore;
    }

    public async Task<ReportSignatureContext?> ResolveAsync(
        GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Signatures is not null)
            return request.Signatures;

        var fromData = HemoproSignatureStore.TryResolveFromData(request.ReportTemplateId, request.Data);
        if (fromData is not null)
            return fromData;

        if (string.IsNullOrWhiteSpace(request.EntityId))
            return null;

        return await _signatureStore.GetAsync(
            request.ReportTemplateId,
            request.EntityId,
            request.TenantCode,
            cancellationToken);
    }
}
