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
    /// <summary>Max wrapped lines for a dialysis-record Note cell before ellipsis.</summary>
    public const int DialysisNoteMaxLines = 4;
    /// <summary>Slightly tighter than data rows so checkbox bands fit one A4 page.</summary>
    public const float CheckRowHeightMm = 4.2f;
    public const float HeaderBarHeightMm = 5.0f;
    public const float TitleHeightMm = 18.4f;
    public const float PageMarginMm = 2f;
    public const float ContentWidthMm = 205.7f;
    public const float AssessmentColumnWidthMm = 100f;
    /// <summary>Left half of Hemodialysis Prescription — also Anticoagulant width so verticals align.</summary>
    public const float PrescriptionLeftColumnWidthMm = 51f;
    /// <summary>Extra air under last top-band rows before Nursing Plan (Telerik has more open space).</summary>
    public const float SectionBreathingMm = 3.0f;
    /// <summary>Looser leading for Time Dialysis rows.</summary>
    public const float TimeDialysisRowHeightMm = 5.8f;
}
