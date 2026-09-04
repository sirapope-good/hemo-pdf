using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

/// <summary>
/// Fragment presets: seed from <c>assets/templates/presets/fragments</c>;
/// Studio saves/overrides under <c>packages/library/fragments</c>.
/// </summary>
public sealed class HprpFragmentPresetStore
{
    private readonly string _seedRoot;
    private readonly string _libraryRoot;

    public HprpFragmentPresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _seedRoot = HprpTemplatePaths.FragmentPresetsRoot(templatesRoot);
        _libraryRoot = HprpTemplatePaths.LibraryFragmentsRoot(
            HprpDiskPaths.ResolvePackagesWriteRoot(options.Value));
    }

    public string LibraryRoot => _libraryRoot;

    public IReadOnlyList<HprpFragmentPreset> ListAll()
    {
        var map = new Dictionary<string, HprpFragmentPreset>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in ReadDir(_seedRoot))
            map[preset.Id] = preset;
        foreach (var preset in ReadDir(_libraryRoot))
            map[preset.Id] = preset;

        return map.Values
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public HprpFragmentPreset? TryGet(string presetId)
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

    public IReadOnlyDictionary<string, HprpFragmentPreset> LoadDictionary()
    {
        return ListAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(HprpFragmentPreset preset, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Id);
        var errors = HprpFragmentValidator.Validate(preset);
        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid fragment: " + string.Join(" ", errors));

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
            return LibraryPresetDeleteResult.NotFound(presetId, "fragment");

        var id = presetId.Trim();
        var libraryPath = Path.Combine(_libraryRoot, id + ".json");
        var seedPath = Path.Combine(_seedRoot, id + ".json");
        var hasSeed = File.Exists(seedPath);

        if (!File.Exists(libraryPath))
        {
            if (hasSeed)
                return LibraryPresetDeleteResult.SeedOnly(id, "fragment");
            return LibraryPresetDeleteResult.NotFound(id, "fragment");
        }

        File.Delete(libraryPath);
        return LibraryPresetDeleteResult.Deleted(id, libraryPath, fellBackToSeed: hasSeed, kind: "fragment");
    }

    public bool IsInLibrary(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return false;
        return File.Exists(Path.Combine(_libraryRoot, presetId.Trim() + ".json"));
    }

    private static IEnumerable<HprpFragmentPreset> ReadDir(string root)
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

    private static HprpFragmentPreset? TryReadFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HprpFragmentPreset>(json, HprpJson.Options);
        }
        catch
        {
            return null;
        }
    }
}
