using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpStudioPackageDto
{
    public HprpManifest Manifest { get; set; } = new();
    public HprpLayout Layout { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Labels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HprpPackage ToPackage(string? sourcePath = null)
    {
        var labels = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (language, map) in Labels)
        {
            if (string.IsNullOrWhiteSpace(language) || map is null)
                continue;
            labels[language] = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
        }

        return new HprpPackage
        {
            Manifest = Manifest,
            Layout = Layout,
            LabelsByLanguage = labels,
            SourcePath = sourcePath ?? "",
        };
    }

    public static HprpStudioPackageDto FromPackage(HprpPackage package)
    {
        var labels = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (language, map) in package.LabelsByLanguage)
            labels[language] = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);

        return new HprpStudioPackageDto
        {
            Manifest = package.Manifest,
            Layout = package.Layout,
            Labels = labels,
        };
    }
}

public sealed class HprpStudioListItemDto
{
    public required string Id { get; init; }
    public string? Variant { get; init; }
    public required string DisplayName { get; init; }
    public required string SourcePath { get; init; }
    public bool Packed { get; init; }
}

public static class HprpStudioCatalog
{
    public static object Describe() => new
    {
        engineVersion = HprpEngine.CurrentVersion,
        fileExtension = HprpEngine.FileExtension,
        widgets = HprpWidgetIds.All.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        blockTypes = HprpWidgetIds.BlockTypes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        dataAdapters = HprpDataAdapterIds.All.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        layoutKinds = HprpLayoutKinds.All.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        entryModes = HprpManifestUi.EntryModes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        parameterSources = HprpManifestUi.ParameterSources.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
        chrome = new
        {
            headerFill = "#RRGGBB or $branding.sectionHeaderBackground",
            border = new[] { "none", "thin", "medium" },
            fontSize = "number",
            rowHeightMm = "number",
            columnWidths = "array of number or *",
        },
    };
}
