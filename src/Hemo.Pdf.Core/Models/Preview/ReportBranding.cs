namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportBranding
{
    public string? LogoUrl { get; init; }
    public IReadOnlyList<string> CompanyLines { get; init; } = [];
    public string Alignment { get; init; } = "center";
}
