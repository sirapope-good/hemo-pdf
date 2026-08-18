namespace Hemo.Pdf.Core.Hprp;

public static class HprpLabels
{
    public static string Get(IReadOnlyDictionary<string, string>? labels, string key, string fallback)
    {
        if (labels is not null
            && labels.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fallback;
    }

    public static IReadOnlyDictionary<string, string> FromPackage(HprpPackage? package, string? language) =>
        package?.GetLabels(language)
        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
