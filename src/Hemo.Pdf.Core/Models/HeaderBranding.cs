namespace Hemo.Pdf.Core.Models;

public enum HeaderAlignment
{
    Left,
    Center,
    Right,
}

public sealed class HeaderBranding
{
    public string? LogoUrl { get; init; }
    public string? LogoPath { get; init; }
    public IReadOnlyList<string> CompanyLines { get; init; } = [];
    public HeaderAlignment TitleAlignment { get; init; } = HeaderAlignment.Center;
    public string? ReportCodePrefix { get; init; }
    public bool ShowPageNumber { get; init; } = true;
}
