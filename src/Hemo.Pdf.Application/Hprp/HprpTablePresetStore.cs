using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpTablePresetStore
{
    private readonly string _presetsRoot;

    public HprpTablePresetStore(IOptions<HprpTemplateOptions> options)
    {
        var templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
        _presetsRoot = HprpTemplatePaths.TablePresetsRoot(templatesRoot);
    }

    public IReadOnlyList<HprpTablePreset> ListAll()
    {
        if (!Directory.Exists(_presetsRoot))
            return [];

        var list = new List<HprpTablePreset>();
        foreach (var file in Directory.EnumerateFiles(_presetsRoot, "*.json"))
        {
            var preset = TryReadFile(file);
            if (preset is not null)
                list.Add(preset);
        }

        return list.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public HprpTablePreset? TryGet(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return null;

        var path = Path.Combine(_presetsRoot, presetId.Trim() + ".json");
        return File.Exists(path) ? TryReadFile(path) : null;
    }

    public IReadOnlyDictionary<string, HprpTablePreset> LoadDictionary()
    {
        return ListAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(HprpTablePreset preset, CancellationToken cancellationToken = default)
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
