using Hemo.Pdf.Application.Mock;

namespace Hemo.Pdf.Api.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MockTenantContextAccessor tenantContextAccessor)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Code", out var tenantHeader)
            && !string.IsNullOrWhiteSpace(tenantHeader))
        {
            var tenantCode = tenantHeader.ToString();
            tenantContextAccessor.SetTenantCode(tenantCode);
            context.Items[MockTenantContextAccessor.HttpContextItemKey] = tenantCode;
        }

        await _next(context);
    }
}
