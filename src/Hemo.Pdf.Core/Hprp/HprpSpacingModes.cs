namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Designer block gap modes for <see cref="HprpPage.SpacingMode"/>.
/// </summary>
public static class HprpSpacingModes
{
    /// <summary>Gap equals page margin (uniform <c>marginMm</c> / resolved sides).</summary>
    public const string Margin = "margin";

    /// <summary>Use <c>spacingMm</c> / <c>spacingBelowMm</c> / <c>spacingBesideMm</c>.</summary>
    public const string Custom = "custom";

    /// <summary>No gap; adjacent borders overlap to look like one line.</summary>
    public const string None = "none";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Margin,
        Custom,
        None,
    };

    public static bool IsKnown(string? mode) =>
        string.IsNullOrWhiteSpace(mode) || All.Contains(mode.Trim());
}
