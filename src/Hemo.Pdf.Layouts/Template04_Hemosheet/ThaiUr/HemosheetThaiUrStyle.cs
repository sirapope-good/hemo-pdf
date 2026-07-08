using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Pixel-parity style profile for the ThaiUR "Hemodialysis Record" Hemosheet layout,
/// transcribed from Hemo-Report/Hemosheet-ThaiUR.trdp (see .cursor/docs/hemosheet-thaiur-spec.md).
///
/// Lives alongside the ThaiUR composer so layout code can reference style tokens without
/// cross-assembly IDE resolution issues.
/// </summary>
internal static class HemosheetThaiUrStyle
{
    // Latin text uses Microsoft Sans Serif (present on Windows hosts); Thai glyphs fall back to Sarabun.
    public const string FontFamily = "Microsoft Sans Serif";
    public const string ThaiFallbackFamily = PdfStyleDefaults.Fonts.PrimaryFamily; // Sarabun

    public const float BaseFontSize = 7.5f;
    public const float UnitFontSize = 5.5f;
    public const float TitleFontSize = 18f;

    // Lavender section-header bar (rgb 192,192,255).
    public const string HeaderBackground = "#C0C0FF";

    // Telerik border width is 0.4pt.
    public const float BorderWidth = 0.4f;

    // Standard field/checkbox row height (0.46cm) and the page density metrics.
    public const float RowHeightMm = 4.6f;
    public const float TitleHeightMm = 18.4f; // logo/title band

    // Page margins (Telerik ~8px). Kept tight so the dense form fits a single A4 page.
    public const float PageMarginMm = 2f;

    // Body content width used by the form (8.1in), in millimetres.
    public const float ContentWidthMm = 205.7f;
}
