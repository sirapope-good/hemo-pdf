using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpHeaderPresetStore
{
    private readonly string _presetsRoot;

    public HprpHeaderPresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _presetsRoot = HprpTemplatePaths.HeaderPresetsRoot(templatesRoot);
    }

    public IReadOnlyList<HprpHeaderPreset> ListAll()
    {
        if (!Directory.Exists(_presetsRoot))
            return [];

        var list = new List<HprpHeaderPreset>();
        foreach (var file in Directory.EnumerateFiles(_presetsRoot, "*.json"))
        {
            var preset = TryReadFile(file);
            if (preset is not null)
                list.Add(preset);
        }

        return list.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public HprpHeaderPreset? TryGet(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        var path = Path.Combine(_presetsRoot, presetId.Trim() + ".json");
        return File.Exists(path) ? TryReadFile(path) : null;
    }

    public IReadOnlyDictionary<string, HprpHeaderPreset> LoadDictionary()
    {
        return ListAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(HprpHeaderPreset preset, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Id);
        Directory.CreateDirectory(_presetsRoot);
        var path = Path.Combine(_presetsRoot, preset.Id.Trim() + ".json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            preset,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
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
}
