using Hemo.Pdf.Branding;
using Hemo.Pdf.Branding.Models;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hemo.Pdf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/branding")]
public sealed class BrandingController : ControllerBase
{
    private readonly IBrandingStore _store;
    private readonly ITenantContextAccessor _tenant;

    public BrandingController(IBrandingStore store, ITenantContextAccessor tenant)
    {
        _store = store;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<BrandingProfileDto>> Get(CancellationToken cancellationToken)
    {
        var tenantCode = RequireTenantCode();
        var profile = await _store.GetByTenantCodeAsync(tenantCode, cancellationToken);
        return Ok(MapDto(profile, tenantCode));
    }

    [HttpPut("style")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<ActionResult<BrandingProfileDto>> PutStyle(
        [FromBody] BrandingStyleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var tenantCode = RequireTenantCode();
        var existing = await _store.GetByTenantCodeAsync(tenantCode, cancellationToken);

        var sectionHeader = ReportSectionHeaderChrome.Normalize(request.SectionHeaderBackground);
        if (request.SectionHeaderBackground is not null
            && request.SectionHeaderBackground.Trim().Length > 0
            && sectionHeader is null)
        {
            return BadRequest(new { message = "sectionHeaderBackground must be a hex color like #C0C0FF." });
        }

        var accent = ReportSectionHeaderChrome.Normalize(request.AccentColor);
        if (request.AccentColor is not null
            && request.AccentColor.Trim().Length > 0
            && accent is null)
        {
            return BadRequest(new { message = "accentColor must be a hex color like #1A5276." });
        }

        var updated = new CustomerBrandingProfile
        {
            TenantCode = tenantCode,
            CustomerId = string.IsNullOrWhiteSpace(existing.CustomerId) ? tenantCode : existing.CustomerId,
            DisplayName = string.IsNullOrWhiteSpace(existing.DisplayName) ? tenantCode : existing.DisplayName,
            Header = existing.Header,
            Footer = existing.Footer,
            HeaderSectionOverride = existing.HeaderSectionOverride,
            Style = new BrandingStyle
            {
                PrimaryFontFamily = string.IsNullOrWhiteSpace(request.PrimaryFontFamily)
                    ? existing.Style?.PrimaryFontFamily ?? "Sarabun"
                    : request.PrimaryFontFamily.Trim(),
                AccentColor = request.AccentColor is null
                    ? existing.Style?.AccentColor
                    : accent,
                SectionHeaderBackground = request.SectionHeaderBackground is null
                    ? existing.Style?.SectionHeaderBackground
                    : sectionHeader,
            },
        };

        await _store.SaveAsync(updated, cancellationToken);
        return Ok(MapDto(updated, tenantCode));
    }

    private string RequireTenantCode()
    {
        if (string.IsNullOrWhiteSpace(_tenant.TenantCode))
            throw new InvalidOperationException("Tenant code is required.");
        return _tenant.TenantCode.Trim();
    }

    private static BrandingProfileDto MapDto(CustomerBrandingProfile profile, string tenantCode) =>
        new()
        {
            TenantCode = string.IsNullOrWhiteSpace(profile.TenantCode) ? tenantCode : profile.TenantCode,
            CustomerId = profile.CustomerId,
            DisplayName = profile.DisplayName,
            Style = new BrandingStyleDto
            {
                PrimaryFontFamily = profile.Style?.PrimaryFontFamily ?? "Sarabun",
                AccentColor = profile.Style?.AccentColor,
                SectionHeaderBackground = profile.Style?.SectionHeaderBackground,
            },
        };
}

public sealed class BrandingProfileDto
{
    public string TenantCode { get; init; } = "";
    public string CustomerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public BrandingStyleDto Style { get; init; } = new();
}

public sealed class BrandingStyleDto
{
    public string PrimaryFontFamily { get; init; } = "Sarabun";
    public string? AccentColor { get; init; }
    public string? SectionHeaderBackground { get; init; }
}

public sealed class BrandingStyleUpdateRequest
{
    public string? PrimaryFontFamily { get; init; }
    public string? AccentColor { get; init; }

    /// <summary>Pass empty string to clear the override (layout defaults apply).</summary>
    public string? SectionHeaderBackground { get; init; }
}
