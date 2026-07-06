using Hemo.Pdf.Core.Abstractions;

namespace Hemo.Pdf.Application.Mock;

public sealed class MockTenantContextAccessor : ITenantContextAccessor
{
    public const string DefaultTenantCode = "tenant-demo-a";
    public const string HttpContextItemKey = "HemoPdf.TenantCode";

    private string? _tenantCode;

    public string TenantCode => _tenantCode ?? DefaultTenantCode;

    public void SetTenantCode(string tenantCode)
    {
        if (!string.IsNullOrWhiteSpace(tenantCode))
            _tenantCode = tenantCode.Trim();
    }
}
