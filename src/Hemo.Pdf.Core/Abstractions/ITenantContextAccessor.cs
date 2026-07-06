namespace Hemo.Pdf.Core.Abstractions;

public interface ITenantContextAccessor
{
    string TenantCode { get; }
}
