using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Abstractions;

public interface IBrandingResolver
{
    Task<CustomerBrandingProfile> ResolveAsync(string tenantCode, CancellationToken cancellationToken);
}
