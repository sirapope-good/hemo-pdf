using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application.Mock;

public sealed class RequestSignatureStore : ISignatureStore
{
    public Task<ReportSignatureContext> GetAsync(
        string reportTemplateId,
        string entityId,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ReportSignatureContext
        {
            IsFullySigned = false,
            Signatures = [],
        });
    }
}
