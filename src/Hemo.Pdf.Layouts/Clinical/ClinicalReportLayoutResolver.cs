using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical;

public enum ClinicalLayoutKind
{
    /// <summary>
    /// Block-flow planner path (unique tenant designs such as Rama).
    /// </summary>
    UniquePlanner,

    /// <summary>
    /// Dense single-page Hemodialysis Record form (clinical-03).
    /// Borrowed from the ThaiUR override as the pack Default until Default is refined separately.
    /// Used when profile is Default (no unique design) or ThaiUr.
    /// </summary>
    HemosheetForm,
}

/// <summary>
/// Chooses layout path for the clinical report pack / hemosheet engine.
/// </summary>
/// <remarks>
/// DEV: <see cref="HemosheetLayoutProfile"/> currently comes from tenant
/// <c>GlobalSetting.Hemosheet.Report.HemosheetTemplate</c> (.trdp filename via
/// HemosheetTemplateCatalog). That same profile is the temporary pointer for which
/// customer structure the whole clinical pack (16 reports) uses.
/// TODO(prod): resolve report pack / layout profile from <c>tenantCode</c> (or a
/// dedicated setting) — do not rely on hemosheet .trdp filename before production.
/// </remarks>
public static class ClinicalReportLayoutResolver
{
    /// <summary>
    /// clinical-03: Default and ThaiUr use the shared Hemosheet form (ThaiUR structure as baseline).
    /// Rama keeps the unique planner path. Other clinical ids are not composed here.
    /// </summary>
    public static ClinicalLayoutKind Resolve(string reportTemplateId, HemosheetLayoutProfile profile)
    {
        if (!ClinicalReportCatalog.IsHemodialysisRecord(reportTemplateId))
            return ClinicalLayoutKind.UniquePlanner;

        // Unique customer design (RAMA consent layout, etc.)
        if (profile == HemosheetLayoutProfile.Rama)
            return ClinicalLayoutKind.UniquePlanner;

        // Default (Hemosheet.trdp) and ThaiUr both use the ThaiUR-borrowed form for now.
        // ThaiUr remains the override source; Default will be refined later.
        return ClinicalLayoutKind.HemosheetForm;
    }

    public static bool UsesHemosheetForm(string reportTemplateId, HemosheetLayoutProfile profile) =>
        Resolve(reportTemplateId, profile) == ClinicalLayoutKind.HemosheetForm;
}
