using Hemo.Pdf.Branding.Models;

namespace Hemo.Pdf.Branding;

public interface IBrandingStore
{
    Task<CustomerBrandingProfile> GetByTenantCodeAsync(string tenantCode, CancellationToken ct);
}
