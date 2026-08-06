using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Sections.Default;

/// <summary>
/// Style profile for clinical-03 Default Hemodialysis Record (CICM baseline).
/// Neutral chrome — no ThaiUR purple/pink section bars.
/// </summary>
public static class HemosheetDefaultStyle
{
    public const string FontFamily = PdfStyleDefaults.Fonts.PrimaryFamily;

    public const float BaseFontSize = 7.5f;
    public const float UnitFontSize = 5.5f;
    public const float DialysisFontSize = 6.5f;
    public const float DialysisUnitFontSize = 5f;
    public const float TitleFontSize = 11f;

    /// <summary>Light cool gray — CICM table headers, not ThaiUR lilac.</summary>
    public const string HeaderBackground = "#DCE3EC";

    public const float BorderWidth = 0.4f;
    public const float RowHeightMm = 4.2f;
    /// <summary>
    /// Patient-card rows — taller than body Rh so Thai diacritics clear the cell
    /// (same reason as <c>HemosheetThaiUrStyle.MetaRowHeightMm</c>).
    /// </summary>
    public const float MetaRowHeightMm = 5.2f;
    /// <summary>Keep dialysis rows fixed-height so note wrap cannot blow the one-page budget.</summary>
    public const int DialysisNoteMaxLines = 1;
    public const int NurseNoteMaxLines = 2;
    public const float CheckRowHeightMm = 3.8f;
    public const float PostCheckRowHeightMm = 3.8f;
    public const float PostStripRowHeightMm = 4.6f;
    public const float HeaderBarHeightMm = 4.5f;
    /// <summary>
    /// Logo/title/patient band. Must stay &gt; 6 × <see cref="MetaRowHeightMm"/> (31.2mm)
    /// or QuestPDF paginates overflowing meta rows onto empty pages.
    /// </summary>
    public const float TitleHeightMm = 33f;
    public const float PageMarginMm = 2f;
    public const float AssessmentColumnWidthMm = 115f;
    public const float PrescriptionLeftColumnWidthMm = 51f;
    public const float SectionBreathingMm = 1.0f;
    public const float TimeDialysisRowHeightMm = 4.8f;
}
