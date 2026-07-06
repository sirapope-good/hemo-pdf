namespace Hemo.Pdf.Application;

public sealed class HemoPdfOptions
{
    public const string SectionName = "HemoPdf";

    public bool UseMockServices { get; set; } = true;

    public string BrandingRootPath { get; set; } = "../../assets/branding";

    public JwtOptions Jwt { get; set; } = new();

    public string[] CorsOrigins { get; set; } = [];
}

public sealed class JwtOptions
{
    public string Authority { get; set; } = "";

    public string Audience { get; set; } = "";
}
