using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpTemplatePaths
{
    public const string ReportsFolder = "reports";
    public const string VariantsFolder = "variants";
    public const string SchemaFolder = "schema";
    public const string SharedFolder = "_shared";
    public const string TenantsFolder = "tenants";
    public const string DefaultVariant = "default";

    public static bool IsReservedFolder(string name) =>
        string.Equals(name, SchemaFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, SharedFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, TenantsFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, ReportsFolder, StringComparison.OrdinalIgnoreCase);

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
