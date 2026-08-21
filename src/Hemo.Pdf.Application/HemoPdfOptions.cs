namespace Hemo.Pdf.Application;

public sealed class HemoPdfOptions
{
    public const string SectionName = "HemoPdf";

    public bool UseMockServices { get; set; } = true;

    /// <summary>
    /// When true, Hemo-PDF fetches report-data from Web.Api (JWT delegation) and ignores client Data/Signatures.
    /// </summary>
    public bool UseServerFetch { get; set; }

    public string BrandingRootPath { get; set; } = "assets/branding";

    /// <summary>Root of unpacked HPRP templates (<c>reports/{id}/</c> and <c>reports/{id}/variants/</c>).</summary>
    public string TemplatesRootPath { get; set; } = "assets/templates";

    public JwtOptions Jwt { get; set; } = new();

    public WebApiOptions WebApi { get; set; } = new();

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

public sealed class WebApiOptions
{
    /// <summary>Base URL of Hemopro Web.Api (e.g. http://localhost:8200).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8200";
}
