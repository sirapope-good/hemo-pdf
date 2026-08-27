namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Experimental layout engines. Default / omitted = composition (<c>body</c>/<c>sections</c>).
/// <c>absolute</c> = freeform widgets with mm coordinates (QuestPDF absolute spike).
/// </summary>
public static class HprpLayoutModes
{
    public const string Composition = "composition";
    public const string Absolute = "absolute";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Composition,
        Absolute,
    };

    public static bool IsAbsolute(HprpManifest? manifest) =>
        manifest is not null
        && string.Equals(manifest.LayoutMode, Absolute, StringComparison.OrdinalIgnoreCase);
}
