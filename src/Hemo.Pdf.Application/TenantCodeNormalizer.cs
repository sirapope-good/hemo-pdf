namespace Hemo.Pdf.Application;

public static class TenantCodeNormalizer
{
    public static string Normalize(string? tenantCode)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return string.Empty;

        var normalized = tenantCode.Trim().ToLowerInvariant();
        return normalized is "localhost" or "127.0.0.1" ? "local" : normalized;
    }

    public static bool EqualsNormalized(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}
