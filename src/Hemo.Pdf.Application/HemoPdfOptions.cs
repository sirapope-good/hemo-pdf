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
    /// <summary>Must match Web.Api Authentication:Issuer. Audience defaults to Issuer when blank.</summary>
    public string Issuer { get; set; } = "";

    /// <summary>Must match Web.Api Authentication:Key (symmetric HS256).</summary>
    public string Key { get; set; } = "";

    /// <summary>Optional. When empty, ValidAudience = Issuer (Hemopro token shape).</summary>
    public string Audience { get; set; } = "";

    /// <summary>Legacy OIDC authority — unused for Hemopro symmetric JWT validation.</summary>
    public string Authority { get; set; } = "";
}
