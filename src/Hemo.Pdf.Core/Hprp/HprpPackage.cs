namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpPackage
{
    public required HprpManifest Manifest { get; init; }
    public required HprpLayout Layout { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LabelsByLanguage { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    public string SourcePath { get; init; } = "";

    public IReadOnlyDictionary<string, string> GetLabels(string? language)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "th" : language.Trim();
        if (LabelsByLanguage.TryGetValue(lang, out var labels))
            return labels;

        if (LabelsByLanguage.TryGetValue("th", out var thai))
            return thai;

        return LabelsByLanguage.Values.FirstOrDefault()
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
