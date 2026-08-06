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
    /// <summary>Dialysis record table + fluid summary — slightly denser than body text.</summary>
    public const float DialysisFontSize = 6.5f;
    public const float DialysisUnitFontSize = 5f;
    public const float TitleFontSize = 18f;

    public const string HeaderBackground = "#C0C0FF";

    public const float BorderWidth = 0.4f;

    public const float RowHeightMm = 4.6f;
    /// <summary>
    /// Patient meta / Diagnosis rows — taller than body Rh so Thai diacritics clear cell borders.
    /// </summary>
    public const float MetaRowHeightMm = 5.4f;
    /// <summary>Max wrapped lines for a dialysis-record Note cell before ellipsis.</summary>
    public const int DialysisNoteMaxLines = 4;
    /// <summary>Max wrapped lines for a Pre/Post nurse-note row before ellipsis.</summary>
    public const int NurseNoteMaxLines = 4;
    /// <summary>Slightly tighter than data rows so checkbox bands fit one A4 page.</summary>
    public const float CheckRowHeightMm = 4.2f;
    /// <summary>Post-assessment checklist rows (Complication / Nursing / Health) — a touch airier.</summary>
    public const float PostCheckRowHeightMm = 4.5f;
    /// <summary>Post Vital / AVF / signature strip under notes.</summary>
    public const float PostStripRowHeightMm = 5.2f;
    public const float HeaderBarHeightMm = 5.0f;
    /// <summary>
    /// Logo + title + patient-meta row. Sized to 4× MetaRowHeightMm (Name, CN/Age, Coverage, ID Card)
    /// so Thai name lines are not clipped. Date/HD NO. live in the diagnosis row when enabled.
    /// </summary>
    public const float TitleHeightMm = 21.6f;
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
