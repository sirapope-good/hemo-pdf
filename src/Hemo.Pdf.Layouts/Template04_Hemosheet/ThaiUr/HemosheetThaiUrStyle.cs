using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Pixel-parity style profile for the ThaiUR "Hemodialysis Record" Hemosheet layout,
/// transcribed from Hemo-Report/Hemosheet-ThaiUR.trdp (see .cursor/docs/hemosheet-thaiur-spec.md).
/// </summary>
internal static class HemosheetThaiUrStyle
{
    // Sarabun is embedded via FontRegistration (assets/fonts/sarabun) — required for Thai glyphs
    // in Docker/Linux where Microsoft Sans Serif is unavailable.
    public const string FontFamily = PdfStyleDefaults.Fonts.PrimaryFamily; // Sarabun

    public const float BaseFontSize = 7.5f;
    public const float UnitFontSize = 5.5f;
    public const float TitleFontSize = 18f;

    public const string HeaderBackground = "#C0C0FF";

    public const float BorderWidth = 0.4f;

    public const float RowHeightMm = 4.6f;
    /// <summary>Slightly tighter than data rows so checkbox bands fit one A4 page.</summary>
    public const float CheckRowHeightMm = 4.2f;
    public const float HeaderBarHeightMm = 5.0f;
    public const float TitleHeightMm = 18.4f;
    public const float PageMarginMm = 2f;
    public const float ContentWidthMm = 205.7f;
    public const float AssessmentColumnWidthMm = 100f;
}
