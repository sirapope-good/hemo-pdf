using Hemo.Pdf.Application;
using Hemo.Pdf.Application.Mock;
using Hemo.Pdf.Core.Exceptions;

namespace Hemo.Pdf.Api.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly HemoPdfOptions _options;

    public TenantMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<HemoPdfOptions> options)
    {
        _next = next;
        _environment = environment;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, MockTenantContextAccessor tenantContextAccessor)
    {
        context.Request.Headers.TryGetValue("X-Tenant-Code", out var tenantHeader);

        // Mock auth (Development + UseMockServices) has no tenant_code claim → trust header.
        // Real JWT path requires tenant_code and rejects header mismatch.
        var isMockAuthPath = _environment.IsDevelopment() && _options.UseMockServices;
        var requireTenantClaim = !isMockAuthPath
            && context.User.Identity?.IsAuthenticated == true;

        var effectiveTenant = TenantRequestResolver.ResolveEffectiveTenant(
            context.User,
            tenantHeader.ToString(),
            requireTenantClaim);

        if (!string.IsNullOrWhiteSpace(effectiveTenant))
        {
            tenantContextAccessor.SetTenantCode(effectiveTenant);
            context.Items[MockTenantContextAccessor.HttpContextItemKey] = effectiveTenant;
        }

        await _next(context);
    }
}
