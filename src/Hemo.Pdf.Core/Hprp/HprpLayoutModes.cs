namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Layout engines. Default / omitted = composition (<c>body</c>/<c>sections</c>).
/// <c>designer</c> = configurable table canvas (WYSIWYG Studio).
/// <c>absolute</c> = legacy freeform mm widgets.
/// </summary>
public static class HprpLayoutModes
{
    public const string Composition = "composition";
    public const string Designer = "designer";
    public const string Absolute = "absolute";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Composition,
        Designer,
        Absolute,
    };

    public static bool IsAbsolute(HprpManifest? manifest) =>
        manifest is not null
        && string.Equals(manifest.LayoutMode, Absolute, StringComparison.OrdinalIgnoreCase);

    public static bool IsDesigner(HprpManifest? manifest) =>
        manifest is not null
        && string.Equals(manifest.LayoutMode, Designer, StringComparison.OrdinalIgnoreCase);

    public static bool IsNonComposition(HprpManifest? manifest) =>
        IsDesigner(manifest) || IsAbsolute(manifest);
}
