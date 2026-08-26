namespace Hemo.Pdf.Core.Hprp;

public readonly struct HprpPageFallback
{
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }
    public float Left { get; init; }
    public float SpacingMm { get; init; }

    public static HprpPageFallback Uniform(float mm, float spacingMm = 2f) => new()
    {
        Top = mm,
        Right = mm,
        Bottom = mm,
        Left = mm,
        SpacingMm = spacingMm,
    };
}

public readonly struct HprpResolvedPage
{
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }
    public float Left { get; init; }
    public float SpacingMm { get; init; }
    public float? FontSize { get; init; }

    public float Vertical => Top + Bottom;
}

/// <summary>
/// File page chrome wins when set; omitted sides keep the composer fallback
/// (hemosheet 2mm, form ReportPageLayout).
/// </summary>
public static class HprpPageLayout
{
    public static HprpResolvedPage Resolve(HprpPage? page, in HprpPageFallback fallback)
    {
        var sides = page?.Margin;
        var shorthand = page?.MarginMm is > 0 and < HprpBox.MaxMm ? page.MarginMm : null;

        float Side(float? named, float fallbackValue) =>
            named ?? shorthand ?? fallbackValue;

        var font = page?.FontSize is > 0 and < 48 ? page.FontSize : null;
        var spacing = page?.SpacingMm is >= 0 and <= HprpBox.MaxMm
            ? page.SpacingMm.Value
            : fallback.SpacingMm;

        return new HprpResolvedPage
        {
            Top = Side(sides?.Top, fallback.Top),
            Right = Side(sides?.Right, fallback.Right),
            Bottom = Side(sides?.Bottom, fallback.Bottom),
            Left = Side(sides?.Left, fallback.Left),
            SpacingMm = spacing,
            FontSize = font,
        };
    }

    public static HprpResolvedPage FromPackage(HprpPackage? package, in HprpPageFallback fallback) =>
        Resolve(package?.Layout.Page, fallback);

    public static void Validate(HprpPage? page, List<string> errors)
    {
        if (page is null)
            return;

        if (page.MarginMm is < 0 or > HprpBox.MaxMm)
            errors.Add("page.marginMm must be between 0 and 80.");

        HprpBox.ValidateSides(page.Margin, "page.margin", errors);

        if (page.SpacingMm is < 0 or > HprpBox.MaxMm)
            errors.Add("page.spacingMm must be between 0 and 80.");

        if (page.FontSize is <= 0 or >= 48)
            errors.Add("page.fontSize must be between 0 and 48.");
    }
}
