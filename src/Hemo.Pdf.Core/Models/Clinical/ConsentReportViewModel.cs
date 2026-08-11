using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>View model for clinical-08 / clinical-09 patient consent forms.</summary>
public sealed class ConsentReportViewModel
{
    public string ConsentId { get; set; } = string.Empty;
    public string ReportTemplateId { get; set; } = string.Empty;
    public string Language { get; set; } = "th";
    public string Type { get; set; } = "Treatment";
    public string Title { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public string? LogoBase64 { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientHn { get; set; } = string.Empty;
    public string CoverageScheme { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string IdentityNumber { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public IReadOnlyList<string> Allergies { get; set; } = [];
    public string SignedByName { get; set; } = string.Empty;
    public bool IsRepresentative { get; set; }
    /// <summary>Patient gender wire value (<c>M</c>/<c>F</c>) for title highlighting on #08.</summary>
    public string? PatientGender { get; set; }
    /// <summary>Relationship to patient when signing as representative (paper dotted line).</summary>
    public string? Relationship { get; set; }
    /// <summary>Free-text "other" reason when acting as representative.</summary>
    public string? RepresentativeReasonOther { get; set; }
    public ConsentDateParts SignedDate { get; set; } = new();
    public ConsentDateParts? ExpiryDate { get; set; }
    public int ExpiryMonths { get; set; }
    public IReadOnlyList<ConsentParagraph> BodyParagraphs { get; set; } = [];
    public string? PatientSignatureBase64 { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSignatureBase64 { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public string? NurseSignatureBase64 { get; set; }
    public string WitnessName { get; set; } = string.Empty;
    public string? WitnessSignatureBase64 { get; set; }

    /// <summary>
    /// New-consent example layout: fill/sign zones render as "..." (header patient identity still shown).
    /// </summary>
    public bool SkeletonExample { get; set; }

    /// <summary>
    /// Minimal hemosheet VM for shared <c>ThaiUrReportHeader</c>
    /// (same chrome as clinical-01…03: logo | title | Name/CN/Age/Coverage/ID + Diagnosis/Drug Allergy).
    /// </summary>
    public HemosheetReportViewModel Header { get; set; } = new();
}

public sealed class ConsentDateParts
{
    public string Day { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
}

public sealed class ConsentParagraph
{
    public string Text { get; set; } = string.Empty;
    public bool Sub { get; set; }
}
