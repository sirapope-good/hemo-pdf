using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Application.Hprp;

public static class HprpStudioSamplePayloads
{
    private static readonly Regex SafeScenario = new("^[a-z0-9-]{1,32}$", RegexOptions.CultureInvariant);

    public static readonly IReadOnlyList<string> KnownTemplateIds =
        ClinicalReportCatalog.All.Select(d => d.Id).ToList();

    public static JsonElement? TryLoad(
        string templatesRoot,
        string templateId,
        string? variant = null,
        string? scenario = null)
    {
        if (string.IsNullOrWhiteSpace(templatesRoot) || string.IsNullOrWhiteSpace(templateId))
            return null;

        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var path = ResolveSamplePath(templatesRoot, id, scenario);
        if (path is null || !File.Exists(path))
            return null;

        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        if (!ClinicalReportCatalog.IsHemodialysisRecord(id))
            return doc.RootElement.Clone();

        return ApplyHemosheetPreviewContext(doc.RootElement, overlay: null, variant);
    }

    /// <summary>
    /// Lists scenario ids for Studio (file stem after <c>sample.</c>, plus default empty for sample.json).
    /// </summary>
    public static IReadOnlyList<string> ListScenarios(string templatesRoot, string templateId)
    {
        if (string.IsNullOrWhiteSpace(templatesRoot) || string.IsNullOrWhiteSpace(templateId))
            return [];

        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var dir = Path.Combine(templatesRoot, "reports", id);
        if (!Directory.Exists(dir))
            return [];

        var list = new List<string>();
        if (File.Exists(Path.Combine(dir, "sample.json")))
            list.Add("");

        foreach (var file in Directory.EnumerateFiles(dir, "sample.*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith("sample.", StringComparison.OrdinalIgnoreCase))
            {
                var scenario = name["sample.".Length..];
                if (SafeScenario.IsMatch(scenario))
                    list.Add(scenario);
            }
        }

        return list;
    }

    private static string? ResolveSamplePath(string templatesRoot, string templateId, string? scenario)
    {
        var dir = Path.Combine(templatesRoot, "reports", templateId);
        if (string.IsNullOrWhiteSpace(scenario))
            return Path.Combine(dir, "sample.json");

        var id = scenario.Trim().ToLowerInvariant();
        if (!SafeScenario.IsMatch(id))
            return null;

        return Path.Combine(dir, $"sample.{id}.json");
    }

    /// <summary>
    /// Studio preview: layoutProfile must match the opened variant / manifest, not tenant frontend settings.
    /// </summary>
    public static JsonElement ApplyHemosheetPreviewContext(
        JsonElement data,
        HprpPackage? overlay,
        string? variant)
    {
        var root = JsonNode.Parse(data.GetRawText());
        if (root is null)
            return data;

        var layoutContext = root["layoutContext"] as JsonObject
            ?? (JsonObject)(root["layoutContext"] = new JsonObject());

        var profile = ResolveLayoutProfile(overlay?.Manifest, variant);
        layoutContext["layoutProfile"] = profile;

        return JsonSerializer.SerializeToElement(root);
    }

    internal static string ResolveLayoutProfile(HprpManifest? manifest, string? variant)
    {
        if (!string.IsNullOrWhiteSpace(manifest?.LayoutProfile))
            return manifest.LayoutProfile.Trim();

        return LayoutProfileForVariant(variant ?? manifest?.Variant);
    }

    private static string LayoutProfileForVariant(string? variant) =>
        HprpTemplatePaths.NormalizeVariant(variant) switch
        {
            "rama" => "Rama",
            "thaiur" => "ThaiUr",
            _ => "Default",
        };
}
