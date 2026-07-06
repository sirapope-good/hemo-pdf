using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Abstractions;

public interface ISignatureStore
{
    Task<ReportSignatureContext> GetAsync(
        string reportTemplateId,
        string entityId,
        string tenantCode,
        CancellationToken cancellationToken);
}
