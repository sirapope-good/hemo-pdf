using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Layouts.Hprp;

/// <summary>
/// Allowed widget sets + default order per dedicated clinical report.
/// Keep allow-lists tight so unrelated catalog ids cannot sneak into a dense PDF.
/// </summary>
public static class HprpClinicalWidgetSets
{
    public static readonly IReadOnlyList<string> Clinical01DefaultOrder =
    [
        HprpWidgetIds.ThaiUrHeader,
        HprpWidgetIds.ClinicalHctEpoAnnualTable,
        HprpWidgetIds.ClinicalHctEpoCopay,
    ];

    public static readonly IReadOnlySet<string> Clinical01Allowed =
        new HashSet<string>(Clinical01DefaultOrder, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<string> Clinical02DefaultOrder =
    [
        HprpWidgetIds.ThaiUrHeader,
        HprpWidgetIds.ClinicalEpoDrugTable,
        HprpWidgetIds.ClinicalHctEpoCopay,
    ];

    public static readonly IReadOnlySet<string> Clinical02Allowed =
        new HashSet<string>(Clinical02DefaultOrder, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<string> Clinical05BodyDefault =
    [
        HprpWidgetIds.ClinicalSoapTable,
    ];

    public static readonly IReadOnlySet<string> Clinical05HeaderAllowed =
        new HashSet<string>([HprpWidgetIds.ThaiUrHeader], StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> Clinical05BodyAllowed =
        new HashSet<string>(Clinical05BodyDefault, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<string> ConsentBodyDefault =
    [
        HprpWidgetIds.ClinicalConsentNarrative,
    ];

    public static readonly IReadOnlySet<string> ConsentHeaderAllowed =
        new HashSet<string>([HprpWidgetIds.ThaiUrHeader], StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> ConsentBodyAllowed =
        new HashSet<string>(ConsentBodyDefault, StringComparer.OrdinalIgnoreCase);
}
