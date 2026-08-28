using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpFragmentPresetStore
{
    private readonly string _presetsRoot;

    public HprpFragmentPresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _presetsRoot = HprpTemplatePaths.FragmentPresetsRoot(templatesRoot);
    }

    public IReadOnlyList<HprpFragmentPreset> ListAll()
    {
        if (!Directory.Exists(_presetsRoot))
            return [];

        var list = new List<HprpFragmentPreset>();
        foreach (var file in Directory.EnumerateFiles(_presetsRoot, "*.json"))
        {
            var preset = TryReadFile(file);
            if (preset is not null)
                list.Add(preset);
        }

        return list.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public HprpFragmentPreset? TryGet(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        var path = Path.Combine(_presetsRoot, presetId.Trim() + ".json");
        return File.Exists(path) ? TryReadFile(path) : null;
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

        Directory.CreateDirectory(_presetsRoot);
        var path = Path.Combine(_presetsRoot, preset.Id.Trim() + ".json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            preset,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
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
