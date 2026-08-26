using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Constants;

/// <summary>
/// Central clinical report pack (17 documents; baseline: thaiur-reports 01–16 + checklist).
/// Canonical engine ids for Hemo-PDF. Legacy aliases (<c>hemosheet</c>,
/// <c>template-04-hemosheet</c>) still normalize to <see cref="HemodialysisRecord"/>.
/// </summary>
public static class ClinicalReportCatalog
{
    public const string HctEpo = "clinical-01-hct-epo";
    public const string EpoDrug = "clinical-02-epo-drug";
    public const string HemodialysisRecord = "clinical-03-hemodialysis-record";
    public const string Prescription = "clinical-04-prescription";
    public const string ProgressNote = "clinical-05-progress-note";
    public const string ProgressNoteChecklist = "clinical-05-progress-note-checklist";
    public const string Medication = "clinical-06-medication";
    public const string Lab = "clinical-07-lab";
    public const string ConsentTh = "clinical-08-consent-th";
    public const string ConsentEn = "clinical-09-consent-en";
    public const string PatientData = "clinical-10-patient-data";
    public const string Admission = "clinical-11-admission";
    public const string EducationTh = "clinical-12-education-th";
    public const string EducationEn = "clinical-13-education-en";
    public const string MarMonth = "clinical-14-mar-month";
    public const string HdSummary = "clinical-15-hd-summary";
    public const string AdequacySummary = "clinical-16-adequacy-summary";

    /// <summary>Legacy Hemopro document-type key (reports route / Telerik era).</summary>
    public const string LegacyDocumentTypeAlias = "hemosheet";

    /// <summary>Legacy hemo-pdf engine id from early template-01…12 registry.</summary>
    public const string LegacyEngineAlias = "template-04-hemosheet";

    private static readonly IReadOnlyDictionary<string, ReportTemplateDefinition> Definitions =
        new Dictionary<string, ReportTemplateDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [HctEpo] = new() { Id = HctEpo, DisplayName = "Hemodialysis Review Hct and EPO", RequiresSignature = false },
            [EpoDrug] = new() { Id = EpoDrug, DisplayName = "Erythropoietin Drug Record", RequiresSignature = false },
            [HemodialysisRecord] = new() { Id = HemodialysisRecord, DisplayName = "Hemodialysis Record", RequiresSignature = true },
            [Prescription] = new() { Id = Prescription, DisplayName = "Hemodialysis Prescription", RequiresSignature = true },
            [ProgressNote] = new() { Id = ProgressNote, DisplayName = "Hemodialysis Progress note", RequiresSignature = true },
            [ProgressNoteChecklist] = new() { Id = ProgressNoteChecklist, DisplayName = "Hemodialysis Progress note", RequiresSignature = false },
            [Medication] = new() { Id = Medication, DisplayName = "Medication Record", RequiresSignature = false },
            [Lab] = new() { Id = Lab, DisplayName = "Laboratory Record", RequiresSignature = false },
            // Signatures are embedded images in report-data (not hemosheet signing workflow).
            [ConsentTh] = new() { Id = ConsentTh, DisplayName = "Treatment Consent (TH)", RequiresSignature = false },
            [ConsentEn] = new() { Id = ConsentEn, DisplayName = "Treatment Consent (EN)", RequiresSignature = false },
            [PatientData] = new() { Id = PatientData, DisplayName = "Patient Data Record", RequiresSignature = false },
            [Admission] = new() { Id = Admission, DisplayName = "Admission Note and History Review", RequiresSignature = false },
            [EducationTh] = new() { Id = EducationTh, DisplayName = "Hemodialysis Patient Education (TH)", RequiresSignature = false },
            [EducationEn] = new() { Id = EducationEn, DisplayName = "Hemodialysis Patient Education (EN)", RequiresSignature = false },
            [MarMonth] = new() { Id = MarMonth, DisplayName = "Medication Administration Record (PerMonth)", RequiresSignature = false },
            [HdSummary] = new() { Id = HdSummary, DisplayName = "Hemodialysis Summary", RequiresSignature = false },
            [AdequacySummary] = new() { Id = AdequacySummary, DisplayName = "Hemodialysis Adequacy Summary", RequiresSignature = false },
        };

    public static IReadOnlyList<ReportTemplateDefinition> All { get; } = Definitions.Values.ToList();

    public static bool IsKnown(string templateId) =>
        Definitions.ContainsKey(templateId) || IsHemodialysisRecord(templateId);

    public static bool TryGetDefinition(string templateId, out ReportTemplateDefinition? definition)
    {
        if (IsHemodialysisRecord(templateId))
        {
            definition = Definitions[HemodialysisRecord];
            return true;
        }

        return Definitions.TryGetValue(templateId, out definition);
    }

    public static bool RequiresSignature(string templateId) =>
        TryGetDefinition(templateId, out var definition) && definition!.RequiresSignature;

    /// <summary>#03 Hemodialysis Record — canonical or legacy aliases.</summary>
    public static bool IsHemodialysisRecord(string? templateId) =>
        string.Equals(templateId, HemodialysisRecord, StringComparison.OrdinalIgnoreCase)
        || string.Equals(templateId, LegacyDocumentTypeAlias, StringComparison.OrdinalIgnoreCase)
        || string.Equals(templateId, LegacyEngineAlias, StringComparison.OrdinalIgnoreCase);

    /// <summary>Canonical engine id (aliases collapse to <see cref="HemodialysisRecord"/>).</summary>
    public static string ResolveEngineTemplateId(string templateId) =>
        IsHemodialysisRecord(templateId) ? HemodialysisRecord : templateId;

    /// <summary>
    /// Dedicated dense QuestPDF engines with no DOM planner mirror (previewMode = pdf).
    /// Hemodialysis Record (#03) still depends on layout profile — see ReportPreviewService.
    /// </summary>
    public static bool UsesDensePdfPreview(string? templateId)
    {
        var engineId = ResolveEngineTemplateId(templateId ?? string.Empty);
        return string.Equals(engineId, HctEpo, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, EpoDrug, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, Prescription, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ProgressNote, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ProgressNoteChecklist, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, Lab, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, "medicine-preparation-round", StringComparison.OrdinalIgnoreCase)
            || IsConsentReport(engineId);
    }

    public static bool IsConsentReport(string? templateId) =>
        string.Equals(templateId, ConsentTh, StringComparison.OrdinalIgnoreCase)
        || string.Equals(templateId, ConsentEn, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Clinical pack forms that still lack a dedicated dense engine and use
    /// HPRP flatten-dto + convention report-data
    /// (<c>api/Patients/{patientId}/reports/{templateId}/report-data</c>).
    /// Includes Lab (#07) for catalog grouping; Lab still has a dedicated fetch kind.
    /// </summary>
    public static IEnumerable<string> DefaultScaffoldIds =>
        Definitions.Keys.Where(id =>
            !string.Equals(id, HemodialysisRecord, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(id, HctEpo, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(id, EpoDrug, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(id, ProgressNote, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(id, ProgressNoteChecklist, StringComparison.OrdinalIgnoreCase)
            && !IsConsentReport(id));

    /// <summary>
    /// True for pack forms that should use <see cref="ReportDataFetchKind.FormPatientByAdapter"/>
    /// (or Lab's dedicated kind). Formerly blocked as Unsupported; BE endpoints exist for 04/06/07/10–16.
    /// </summary>
    public static bool IsFormPatientReportData(string? templateId)
    {
        var engineId = ResolveEngineTemplateId(templateId ?? string.Empty);
        return DefaultScaffoldIds.Any(id => string.Equals(id, engineId, StringComparison.OrdinalIgnoreCase));
    }
}
