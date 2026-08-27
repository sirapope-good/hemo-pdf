namespace Hemo.Pdf.Core.Hprp;

/// <summary>Resolved designer block gaps (mm).</summary>
public readonly struct HprpDesignerGaps
{
    public float BelowMm { get; init; }
    public float BesideMm { get; init; }

    /// <summary>
    /// When a gap is 0, next block overlaps by this amount so adjacent borders read as one line.
    /// Matches QuestPDF thin border (~0.4pt ≈ 0.14mm); 0.2mm is a stable visual collapse.
    /// </summary>
    public const float BorderCollapseMm = 0.2f;

    public float StepX(float widthMm) =>
        widthMm + (BesideMm > 0 ? BesideMm : -BorderCollapseMm);

    public float StepY(float heightMm) =>
        heightMm + (BelowMm > 0 ? BelowMm : -BorderCollapseMm);
}

public static partial class HprpPageLayout
{
    /// <summary>
    /// Resolve designer gaps from page settings.
    /// <list type="bullet">
    /// <item><c>none</c> → 0 / 0 (borders collapse)</item>
    /// <item><c>margin</c> → equals page margin (uniform or left)</item>
    /// <item><c>custom</c> / omitted → <c>spacingBelowMm</c> / <c>spacingBesideMm</c> / <c>spacingMm</c></item>
    /// </list>
    /// </summary>
    public static HprpDesignerGaps ResolveDesignerGaps(
        HprpPage? page,
        float marginLeftMm,
        float fallbackSpacingMm = 2f)
    {
        var mode = (page?.SpacingMode ?? HprpSpacingModes.Custom).Trim();
        if (string.Equals(mode, HprpSpacingModes.None, StringComparison.OrdinalIgnoreCase))
        {
            return new HprpDesignerGaps { BelowMm = 0, BesideMm = 0 };
        }

        if (string.Equals(mode, HprpSpacingModes.Margin, StringComparison.OrdinalIgnoreCase))
        {
            var m = page?.MarginMm is >= 0 and <= HprpBox.MaxMm
                ? page.MarginMm.Value
                : marginLeftMm;
            return new HprpDesignerGaps { BelowMm = m, BesideMm = m };
        }

        // custom / unknown → custom fields
        float Pick(float? side, float? shared, float fallback) =>
            side is >= 0 and <= HprpBox.MaxMm
                ? side.Value
                : shared is >= 0 and <= HprpBox.MaxMm
                    ? shared.Value
                    : fallback;

        var shared = page?.SpacingMm;
        return new HprpDesignerGaps
        {
            BelowMm = Pick(page?.SpacingBelowMm, shared, fallbackSpacingMm),
            BesideMm = Pick(page?.SpacingBesideMm, shared, fallbackSpacingMm),
        };
    }
}
