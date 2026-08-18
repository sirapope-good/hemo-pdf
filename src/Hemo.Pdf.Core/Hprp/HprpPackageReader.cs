using System.IO.Compression;
using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpPackageReader
{
    public const string ManifestFileName = "manifest.json";
    public const string LayoutFileName = "layout.json";

    public static HprpPackage ReadDirectory(string directory)
    {
        var manifest = ReadJson<HprpManifest>(Path.Combine(directory, ManifestFileName));
        var layout = ReadJson<HprpLayout>(Path.Combine(directory, LayoutFileName));
        var labels = ReadLabelFiles(directory);
        return Build(manifest, layout, labels, directory);
    }

    public static HprpPackage ReadZip(Stream zipStream, string sourcePath = "")
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var files = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .ToDictionary(e => NormalizeEntryName(e.FullName), e => e, StringComparer.OrdinalIgnoreCase);

        if (!files.TryGetValue(ManifestFileName, out var manifestEntry))
            throw new InvalidOperationException($"{ManifestFileName} is missing from the .hprp package.");
        if (!files.TryGetValue(LayoutFileName, out var layoutEntry))
            throw new InvalidOperationException($"{LayoutFileName} is missing from the .hprp package.");

        var manifest = ReadJson<HprpManifest>(manifestEntry);
        var layout = ReadJson<HprpLayout>(layoutEntry);
        var labels = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, entry) in files)
        {
            if (!TryParseLabelFileName(name, out var language))
                continue;

            labels[language] = ReadJson<Dictionary<string, string>>(entry);
        }

        return Build(manifest, layout, labels, sourcePath);
    }

    public static async Task WriteZipAsync(HprpPackage package, Stream output, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        await WriteEntryAsync(archive, ManifestFileName, package.Manifest, cancellationToken);
        await WriteEntryAsync(archive, LayoutFileName, package.Layout, cancellationToken);
        foreach (var (language, labels) in package.LabelsByLanguage)
        {
            await WriteEntryAsync(archive, $"labels.{language}.json", labels, cancellationToken);
        }
    }

    private static HprpPackage Build(
        HprpManifest manifest,
        HprpLayout layout,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> labels,
        string sourcePath)
    {
        var package = new HprpPackage
        {
            Manifest = manifest,
            Layout = layout,
            LabelsByLanguage = labels,
            SourcePath = sourcePath,
        };

        var result = HprpValidator.Validate(package);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "Invalid .hprp package: " + string.Join(" ", result.Errors));
        }

        return package;
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> ReadLabelFiles(string directory)
    {
        var labels = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory, "labels.*.json"))
        {
            var name = Path.GetFileName(file);
            if (!TryParseLabelFileName(name, out var language))
                continue;

            labels[language] = ReadJson<Dictionary<string, string>>(file);
        }

        return labels;
    }

    private static bool TryParseLabelFileName(string fileName, out string language)
    {
        language = "";
        const string prefix = "labels.";
        const string suffix = ".json";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        language = fileName[prefix.Length..^suffix.Length];
        return !string.IsNullOrWhiteSpace(language);
    }

    private static string NormalizeEntryName(string fullName)
    {
        var name = fullName.Replace('\\', '/');
        var slash = name.LastIndexOf('/');
        return slash >= 0 ? name[(slash + 1)..] : name;
    }

    private static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing {Path.GetFileName(path)}", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, HprpJson.Options)
            ?? throw new InvalidOperationException($"Empty JSON: {path}");
    }

    private static T ReadJson<T>(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return JsonSerializer.Deserialize<T>(stream, HprpJson.Options)
            ?? throw new InvalidOperationException($"Empty JSON: {entry.FullName}");
    }

    private static async Task WriteEntryAsync<T>(
        ZipArchive archive,
        string name,
        T value,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, value, HprpJson.Options, cancellationToken);
    }
}
