using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Branding;

public sealed class BrandingResolver : IBrandingResolver
{
    private readonly IBrandingStore _store;

    public BrandingResolver(IBrandingStore store)
    {
        _store = store;
    }

    public async Task<CustomerBrandingProfile> ResolveAsync(
        string tenantCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        var source = await _store.GetByTenantCodeAsync(tenantCode, cancellationToken);
        return MapToCore(source);
    }

    private static CustomerBrandingProfile MapToCore(Models.CustomerBrandingProfile source)
    {
        return new CustomerBrandingProfile
        {
            CustomerId = string.IsNullOrWhiteSpace(source.CustomerId) ? source.TenantCode : source.CustomerId,
            DisplayName = source.DisplayName,
            Header = new HeaderBranding
            {
                LogoUrl = source.Header.LogoUrl,
                LogoPath = source.Header.LogoPath,
                CompanyLines = source.Header.CompanyLines,
                TitleAlignment = ParseAlignment(source.Header.TitleAlignment),
                ReportCodePrefix = source.Header.ReportCodePrefix,
                ShowPageNumber = source.Header.ShowPageNumber,
            },
            Footer = new FooterBranding
            {
                DisclaimerText = source.Footer.DisclaimerText,
                ShowSignatures = source.Footer.ShowSignatures,
            },
            Style = new BrandingStyle
            {
                PrimaryFontFamily = source.Style?.PrimaryFontFamily ?? "Sarabun",
                AccentColor = source.Style?.AccentColor,
            },
            HeaderSectionOverride = source.HeaderSectionOverride,
        };
    }

    private static HeaderAlignment ParseAlignment(string? alignment) =>
        alignment?.Trim().ToLowerInvariant() switch
        {
            "left" => HeaderAlignment.Left,
            "right" => HeaderAlignment.Right,
            _ => HeaderAlignment.Center,
        };
}
