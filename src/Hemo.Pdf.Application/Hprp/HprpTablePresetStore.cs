using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

/// <summary>
/// Table presets: seed from <c>assets/templates/presets/tables</c>;
/// Studio saves/overrides under <c>packages/library/tables</c>.
/// </summary>
public sealed class HprpTablePresetStore
{
    private readonly string _seedRoot;
    private readonly string _libraryRoot;

    public HprpTablePresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _seedRoot = HprpTemplatePaths.TablePresetsRoot(templatesRoot);
        _libraryRoot = HprpTemplatePaths.LibraryTablesRoot(
            HprpDiskPaths.ResolvePackagesWriteRoot(options.Value));
    }

    public string LibraryRoot => _libraryRoot;

    public IReadOnlyList<HprpTablePreset> ListAll()
    {
        var map = new Dictionary<string, HprpTablePreset>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in ReadDir(_seedRoot))
            map[preset.Id] = preset;
        foreach (var preset in ReadDir(_libraryRoot))
            map[preset.Id] = preset;

        return map.Values
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public HprpTablePreset? TryGet(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        var id = presetId.Trim();
        var libraryPath = Path.Combine(_libraryRoot, id + ".json");
        if (File.Exists(libraryPath))
            return TryReadFile(libraryPath);

        var seedPath = Path.Combine(_seedRoot, id + ".json");
        return File.Exists(seedPath) ? TryReadFile(seedPath) : null;
    }

    public IReadOnlyDictionary<string, HprpTablePreset> LoadDictionary()
    {
        return ListAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(HprpTablePreset preset, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Id);
        var id = preset.Id.Trim();
        Directory.CreateDirectory(_libraryRoot);
        var path = Path.Combine(_libraryRoot, id + ".json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            preset,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
    }

    public LibraryPresetDeleteResult DeleteLibrary(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return LibraryPresetDeleteResult.NotFound(presetId, "table");

        var id = presetId.Trim();
        var libraryPath = Path.Combine(_libraryRoot, id + ".json");
        var seedPath = Path.Combine(_seedRoot, id + ".json");
        var hasSeed = File.Exists(seedPath);

        if (!File.Exists(libraryPath))
        {
            if (hasSeed)
                return LibraryPresetDeleteResult.SeedOnly(id, "table");
            return LibraryPresetDeleteResult.NotFound(id, "table");
        }

        File.Delete(libraryPath);
        return LibraryPresetDeleteResult.Deleted(id, libraryPath, fellBackToSeed: hasSeed, kind: "table");
    }

    public bool IsInLibrary(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return false;
        return File.Exists(Path.Combine(_libraryRoot, presetId.Trim() + ".json"));
    }

    private static IEnumerable<HprpTablePreset> ReadDir(string root)
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

    private static HprpTablePreset? TryReadFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HprpTablePreset>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}

public sealed class HprpAdapterSchemaStore
{
    private readonly string _adaptersRoot;

    public HprpAdapterSchemaStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _adaptersRoot = HprpTemplatePaths.AdaptersRoot(templatesRoot);
    }

    public HprpAdapterSchema? TryGet(string dataAdapterId)
    {
        if (string.IsNullOrWhiteSpace(dataAdapterId))
            return null;

        var path = Path.Combine(_adaptersRoot, dataAdapterId.Trim() + ".schema.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HprpAdapterSchema>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<string> ListAdapterIds()
    {
        if (!Directory.Exists(_adaptersRoot))
            return [];

        return Directory.EnumerateFiles(_adaptersRoot, "*.schema.json")
            .Select(f => Path.GetFileName(f)![..^".schema.json".Length])
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
