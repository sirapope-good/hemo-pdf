using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical;

public enum ClinicalLayoutKind
{
    /// <summary>Block-flow planner (unique designs such as Rama).</summary>
    UniquePlanner,

    /// <summary>ThaiUR dense form (purple section bars) — Hemosheet-ThaiUR.trdp.</summary>
    ThaiUrForm,

    /// <summary>
    /// Default dense form (CICM-style header/content, no ThaiUR purple chrome) — Hemosheet.trdp.
    /// </summary>
    DefaultForm,
}

/// <summary>
/// Chooses layout path for clinical-03 Hemodialysis Record from
/// <see cref="HemosheetLayoutProfile"/> (tenant setting / report-data layoutContext).
/// </summary>
public static class ClinicalReportLayoutResolver
{
    public static ClinicalLayoutKind Resolve(string reportTemplateId, HemosheetLayoutProfile profile)
    {
        if (!ClinicalReportCatalog.IsHemodialysisRecord(reportTemplateId))
            return ClinicalLayoutKind.UniquePlanner;

        return profile switch
        {
            HemosheetLayoutProfile.ThaiUr => ClinicalLayoutKind.ThaiUrForm,
            HemosheetLayoutProfile.Rama => ClinicalLayoutKind.UniquePlanner,
            _ => ClinicalLayoutKind.DefaultForm,
        };
    }

    public static bool UsesDenseForm(string reportTemplateId, HemosheetLayoutProfile profile)
    {
        var kind = Resolve(reportTemplateId, profile);
        return kind is ClinicalLayoutKind.ThaiUrForm or ClinicalLayoutKind.DefaultForm;
    }

    /// <summary>Obsolete name — use <see cref="UsesDenseForm"/>.</summary>
    public static bool UsesHemosheetForm(string reportTemplateId, HemosheetLayoutProfile profile) =>
        UsesDenseForm(reportTemplateId, profile);
}
