using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

/// <summary>
/// Shared rounded checkbox rendered from SVG (checked / unchecked).
/// Source assets: assets/icons/checkbox-checked.svg, checkbox-unchecked.svg.
/// </summary>
public static class PdfCheckbox
{
    public const float DefaultSizePt = 8f;

    /// <summary>viewBox size of the source SVG (15x15).</summary>
    public const float ViewBox = 15f;

    // Keep markup identical to assets/icons/checkbox-*.svg so PDF and preview stay in sync.
    public const string CheckedSvg =
        """
        <svg width="15" height="15" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="0.5" y="0.5" width="14" height="14" rx="1.5" stroke="#66B4E7"/>
        <path d="M3 8.34375L5.4 12C6.6 9.09375 8.6 5.8125 12 3" stroke="#367EB5" stroke-width="1.5"/>
        </svg>
        """;

    public const string UncheckedSvg =
        """
        <svg width="15" height="15" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="0.5" y="0.5" width="14" height="14" rx="1.5" stroke="#A3ADB4"/>
        </svg>
        """;

    public static string GetSvg(bool isChecked) => isChecked ? CheckedSvg : UncheckedSvg;

    public static void Render(RowDescriptor row, bool isChecked, float sizePt = DefaultSizePt)
    {
        // AlignMiddle so the SVG sits on the text baseline of the row (not top-biased).
        Render(row.ConstantItem(sizePt).AlignMiddle(), isChecked, sizePt);
    }

    public static void Render(IContainer container, bool isChecked, float sizePt = DefaultSizePt)
    {
        container
            .Width(sizePt)
            .Height(sizePt)
            .Svg(GetSvg(isChecked))
            .FitArea();
    }
}
