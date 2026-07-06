using Hemo.Pdf.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hemo.Pdf.Integration.Tests;

public sealed class PdfApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        var brandingPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "branding"));

        builder.UseSetting("HemoPdf:UseMockServices", "true");
        builder.UseSetting("HemoPdf:BrandingRootPath", brandingPath);
    }
}
