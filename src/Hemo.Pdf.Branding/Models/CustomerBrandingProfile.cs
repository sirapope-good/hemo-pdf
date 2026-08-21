namespace Hemo.Pdf.Branding.Models;

public sealed class CustomerBrandingProfile
{
    public string TenantCode { get; init; } = "";
    public string CustomerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public HeaderBranding Header { get; init; } = new();
    public FooterBranding Footer { get; init; } = new();
    public BrandingStyle? Style { get; init; }
    public string? HeaderSectionOverride { get; init; }
}

public sealed class HeaderBranding
{
    public string? LogoPath { get; init; }
    public string? LogoUrl { get; init; }
    public IReadOnlyList<string> CompanyLines { get; init; } = Array.Empty<string>();
    public string TitleAlignment { get; init; } = "center";
    public string? ReportCodePrefix { get; init; }
    public bool ShowPageNumber { get; init; } = true;
}

public sealed class FooterBranding
{
    public string? DisclaimerText { get; init; }
    public bool ShowSignatures { get; init; }
}

public sealed class BrandingStyle
{
    public string PrimaryFontFamily { get; init; } = "Sarabun";
    public string? AccentColor { get; init; }
    /// <summary>Column / section header fill for all report widgets (e.g. #C0C0FF).</summary>
    public string? SectionHeaderBackground { get; init; }
}
