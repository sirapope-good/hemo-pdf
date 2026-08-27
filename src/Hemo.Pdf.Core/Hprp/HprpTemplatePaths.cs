using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpTemplatePaths
{
    public const string ReportsFolder = "reports";
    public const string VariantsFolder = "variants";
    public const string SchemaFolder = "schema";
    public const string SharedFolder = "_shared";
    public const string TenantsFolder = "tenants";
    public const string PackagesFolder = "packages";
    public const string PresetsFolder = "presets";
    public const string TablePresetsFolder = "presets/tables";
    public const string HeaderPresetsFolder = "presets/headers";
    public const string AdaptersFolder = "adapters";
    public const string DefaultVariant = "default";
    public const string SolutionFileName = "Hemo.Pdf.sln";

    public static bool IsReservedFolder(string name) =>
        string.Equals(name, SchemaFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, SharedFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, TenantsFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, ReportsFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, PresetsFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, AdaptersFolder, StringComparison.OrdinalIgnoreCase);

    public static bool IsDefaultVariant(string? variant) =>
        string.IsNullOrWhiteSpace(variant)
        || string.Equals(variant.Trim(), DefaultVariant, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeVariant(string? variant) =>
        IsDefaultVariant(variant) ? DefaultVariant : variant!.Trim().ToLowerInvariant();

    public static string FromLayoutProfile(HemosheetLayoutProfile profile) =>
        profile switch
        {
            HemosheetLayoutProfile.Rama => "rama",
            HemosheetLayoutProfile.ThaiUr => "thaiur",
            _ => DefaultVariant,
        };

    public static string CacheKey(string templateId, string? variant) =>
        $"{templateId}#{NormalizeVariant(variant)}";

    public static string ReportsRoot(string templatesRoot) =>
        Path.Combine(templatesRoot, ReportsFolder);

    public static string TablePresetsRoot(string templatesRoot) =>
        Path.Combine(templatesRoot, TablePresetsFolder);

    public static string HeaderPresetsRoot(string templatesRoot) =>
        Path.Combine(templatesRoot, HeaderPresetsFolder);

    public static string AdaptersRoot(string templatesRoot) =>
        Path.Combine(templatesRoot, AdaptersFolder);

    /// <summary>
    /// Packed file name. Single-package reports use <c>{id}.hprp</c>;
    /// variant folders use <c>{id}.{variant}.hprp</c> (including <c>default</c>).
    /// </summary>
    public static string PackageFileName(string templateId, string? variant, bool includeVariantSegment)
    {
        var id = templateId.Trim();
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("templateId is required.", nameof(templateId));

        if (!includeVariantSegment)
            return id + HprpEngine.FileExtension;

        return $"{id}.{NormalizeVariant(variant)}{HprpEngine.FileExtension}";
    }

    public static bool TryParsePackageFileName(string fileName, out string templateId, out string variant)
    {
        templateId = "";
        variant = DefaultVariant;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var name = Path.GetFileName(fileName);
        if (!name.EndsWith(HprpEngine.FileExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        var stem = name[..^HprpEngine.FileExtension.Length];
        if (string.IsNullOrWhiteSpace(stem))
            return false;

        var dot = stem.LastIndexOf('.');
        if (dot <= 0)
        {
            templateId = stem;
            return true;
        }

        templateId = stem[..dot];
        variant = NormalizeVariant(stem[(dot + 1)..]);
        return !string.IsNullOrWhiteSpace(templateId);
    }

    public static string? FindRepoRoot(params string[] startPaths)
    {
        foreach (var start in startPaths)
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;

            var dir = new DirectoryInfo(Path.GetFullPath(start));
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }

        return null;
    }
}

public static class HprpLayoutKinds
{
    public const string DefaultForm = "DefaultForm";
    public const string ThaiUrForm = "ThaiUrForm";
    public const string UniquePlanner = "UniquePlanner";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        DefaultForm,
        ThaiUrForm,
        UniquePlanner,
    };
}
