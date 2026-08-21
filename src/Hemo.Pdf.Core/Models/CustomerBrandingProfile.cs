using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Core.Models;

public sealed class BrandingStyle
{
    public string PrimaryFontFamily { get; init; } = PdfStyleDefaults.Fonts.PrimaryFamily;
    public string? AccentColor { get; init; }
    /// <summary>Column / section header fill for all report widgets (e.g. #C0C0FF).</summary>
    public string? SectionHeaderBackground { get; init; }
}

public sealed class CustomerBrandingProfile
{
    public string CustomerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public HeaderBranding Header { get; init; } = new();
    public FooterBranding Footer { get; init; } = new();
    public BrandingStyle Style { get; init; } = new();
    public string? HeaderSectionOverride { get; init; }
}
