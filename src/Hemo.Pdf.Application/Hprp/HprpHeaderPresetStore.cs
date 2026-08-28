using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

/// <summary>
/// Header presets: seed from <c>assets/templates/presets/headers</c>;
/// Studio saves/overrides under <c>packages/library/headers</c> (same idea as .hprp packs).
/// </summary>
public sealed class HprpHeaderPresetStore
{
    /// <summary>Legacy id → current id (layouts may still reference the old name).</summary>
    private static readonly IReadOnlyDictionary<string, string> IdAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["thaiur-header-v1"] = "clinical-header-thaiur",
        };

    private readonly string _seedRoot;
    private readonly string _libraryRoot;

    public HprpHeaderPresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _seedRoot = HprpTemplatePaths.HeaderPresetsRoot(templatesRoot);
        _libraryRoot = HprpTemplatePaths.LibraryHeadersRoot(
            HprpDiskPaths.ResolvePackagesWriteRoot(options.Value));
    }

    public string LibraryRoot => _libraryRoot;

    public IReadOnlyList<HprpHeaderPreset> ListAll()
    {
        var map = new Dictionary<string, HprpHeaderPreset>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in ReadDir(_seedRoot))
            map[CanonicalId(preset.Id)] = WithCanonicalId(preset)!;
        foreach (var preset in ReadDir(_libraryRoot))
            map[CanonicalId(preset.Id)] = WithCanonicalId(preset)!;

        return map.Values
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public HprpHeaderPreset? TryGet(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        var id = CanonicalId(presetId.Trim());
        var libraryPath = Path.Combine(_libraryRoot, id + ".json");
        if (File.Exists(libraryPath))
            return WithCanonicalId(TryReadFile(libraryPath));

        var seedPath = Path.Combine(_seedRoot, id + ".json");
        if (File.Exists(seedPath))
            return WithCanonicalId(TryReadFile(seedPath));

        var legacyKey = presetId.Trim();
        if (IdAliases.ContainsKey(legacyKey))
        {
            var legacySeed = Path.Combine(_seedRoot, legacyKey + ".json");
            if (File.Exists(legacySeed))
                return WithCanonicalId(TryReadFile(legacySeed));
        }

        return null;
    }

    public IReadOnlyDictionary<string, HprpHeaderPreset> LoadDictionary()
    {
        return ListAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(HprpHeaderPreset preset, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Id);
        var id = CanonicalId(preset.Id.Trim());
        var toSave = CloneWithId(preset, id);
        Directory.CreateDirectory(_libraryRoot);
        var path = Path.Combine(_libraryRoot, id + ".json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            toSave,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes <c>packages/library/headers/{id}.json</c> only — never seed under assets.
    /// If a seed remains, the preset reappears from seed after delete.
    /// </summary>
    public LibraryPresetDeleteResult DeleteLibrary(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return LibraryPresetDeleteResult.NotFound(presetId, "header");

        var id = CanonicalId(presetId.Trim());
        var libraryPath = Path.Combine(_libraryRoot, id + ".json");
        var seedPath = Path.Combine(_seedRoot, id + ".json");
        var hasSeed = File.Exists(seedPath);

        if (!File.Exists(libraryPath))
        {
            if (hasSeed)
                return LibraryPresetDeleteResult.SeedOnly(id, "header");
            return LibraryPresetDeleteResult.NotFound(id, "header");
        }

        File.Delete(libraryPath);
        return LibraryPresetDeleteResult.Deleted(id, libraryPath, fellBackToSeed: hasSeed, kind: "header");
    }

    public bool IsInLibrary(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return false;
        var id = CanonicalId(presetId.Trim());
        return File.Exists(Path.Combine(_libraryRoot, id + ".json"));
    }

    public static string CanonicalId(string presetId)
    {
        if (IdAliases.TryGetValue(presetId, out var mapped))
            return mapped;
        return presetId;
    }

    private static HprpHeaderPreset? WithCanonicalId(HprpHeaderPreset? preset)
    {
        if (preset is null)
            return null;
        var id = CanonicalId(preset.Id);
        return string.Equals(preset.Id, id, StringComparison.Ordinal)
            ? preset
            : CloneWithId(preset, id);
    }

    private static HprpHeaderPreset CloneWithId(HprpHeaderPreset preset, string id)
    {
        var json = JsonSerializer.Serialize(new HeaderPresetWriteDto(preset, id));
        return JsonSerializer.Deserialize<HprpHeaderPreset>(json, HprpJson.Options)!;
    }

    private static IEnumerable<HprpHeaderPreset> ReadDir(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        foreach (var file in Directory.EnumerateFiles(root, "*.json"))
        {
            var preset = TryReadFile(file);
            if (preset is not null)
                yield return preset;
        }
    }

    private static HprpHeaderPreset? TryReadFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HprpHeaderPreset>(json, HprpJson.Options);
        }
        catch
        {
            return null;
        }
    }

    private sealed class HeaderPresetWriteDto
    {
        public HeaderPresetWriteDto(HprpHeaderPreset src, string id)
        {
            Id = id;
            DisplayName = src.DisplayName;
            Tags = src.Tags;
            TitleRowHeightMm = src.TitleRowHeightMm;
            BottomRowHeightMm = src.BottomRowHeightMm;
            ShowDateAndHdNo = src.ShowDateAndHdNo;
            ShowHdPerWeek = src.ShowHdPerWeek;
            Columns = src.Columns;
            MetaLines = src.MetaLines;
            BottomFields = src.BottomFields;
            Chrome = src.Chrome;
        }

        [JsonPropertyName("id")]
        public string Id { get; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; }
        [JsonPropertyName("tags")]
        public IReadOnlyList<string> Tags { get; }
        [JsonPropertyName("titleRowHeightMm")]
        public float TitleRowHeightMm { get; }
        [JsonPropertyName("bottomRowHeightMm")]
        public float BottomRowHeightMm { get; }
        [JsonPropertyName("showDateAndHdNo")]
        public bool ShowDateAndHdNo { get; }
        [JsonPropertyName("showHdPerWeek")]
        public bool ShowHdPerWeek { get; }
        [JsonPropertyName("columns")]
        public IReadOnlyList<HprpHeaderBandColumn> Columns { get; }
        [JsonPropertyName("metaLines")]
        public IReadOnlyList<HprpHeaderFieldLine> MetaLines { get; }
        [JsonPropertyName("bottomFields")]
        public IReadOnlyList<HprpHeaderFieldLine> BottomFields { get; }
        [JsonPropertyName("chrome")]
        public HprpChrome? Chrome { get; }
    }
}
