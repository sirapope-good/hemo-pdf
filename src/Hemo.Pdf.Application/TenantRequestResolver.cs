using System.Security.Claims;
using Hemo.Pdf.Core.Exceptions;

namespace Hemo.Pdf.Application;

public static class TenantRequestResolver
{
    public const string TenantCodeClaimType = "tenant_code";

    /// <summary>
    /// Resolves the trusted tenant for the request. When the principal has a tenant_code claim,
    /// that claim wins and a mismatched X-Tenant-Code header is rejected.
    /// Mock/dev principals without the claim fall back to the header.
    /// </summary>
    public static string ResolveEffectiveTenant(
        ClaimsPrincipal? user,
        string? headerTenantCode,
        bool requireTenantClaim)
    {
        var claimTenant = user?.FindFirst(TenantCodeClaimType)?.Value;
        var hasClaim = !string.IsNullOrWhiteSpace(claimTenant);
        var headerNormalized = TenantCodeNormalizer.Normalize(headerTenantCode);

        if (hasClaim)
        {
            var claimNormalized = TenantCodeNormalizer.Normalize(claimTenant);
            if (!string.IsNullOrEmpty(headerNormalized)
                && !string.Equals(headerNormalized, claimNormalized, StringComparison.Ordinal))
            {
                throw new PdfGenerationForbiddenException(
                    "X-Tenant-Code does not match the authenticated tenant.");
            }

            return claimNormalized;
        }

        if (requireTenantClaim)
        {
            throw new PdfGenerationForbiddenException(
                "Authenticated token is missing required tenant_code claim.");
        }

        if (string.IsNullOrEmpty(headerNormalized))
            return string.Empty;

        return headerNormalized;
    }
}
