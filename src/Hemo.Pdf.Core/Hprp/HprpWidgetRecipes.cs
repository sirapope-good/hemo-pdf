using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpBindField
{
    [JsonPropertyName("bind")]
    public required string Bind { get; init; }

    [JsonPropertyName("labelKey")]
    public string? LabelKey { get; init; }

    [JsonPropertyName("defaultLabel")]
    public string? DefaultLabel { get; init; }
}

public sealed class HprpWidgetRecipe
{
    public const string KindDense = "dense";
    public const string KindBlock = "block";

    public const string SlotBody = "body";
    public const string SlotSections = "sections";

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = KindDense;

    /// <summary>
    /// Studio list slot: clinical reports use <see cref="SlotBody"/>;
    /// hemosheet uses <see cref="SlotSections"/>.
    /// </summary>
    [JsonPropertyName("slot")]
    public string Slot { get; init; } = SlotBody;

    [JsonPropertyName("allowedOn")]
    public IReadOnlyList<string> AllowedOn { get; init; } = [];

    [JsonPropertyName("bindFields")]
    public IReadOnlyList<HprpBindField> BindFields { get; init; } = [];

    [JsonPropertyName("defaultColumnPlan")]
    public IReadOnlyList<HprpColumnPlanItem> DefaultColumnPlan { get; init; } = [];

    /// <summary>Hemosheet dialysis header labels (string[]), not clinical-01 columnPlan.</summary>
    [JsonPropertyName("defaultColumns")]
    public IReadOnlyList<string> DefaultColumns { get; init; } = [];

    [JsonPropertyName("defaultColumnsWhen")]
    public Dictionary<string, string[]>? DefaultColumnsWhen { get; init; }

    [JsonPropertyName("chromeDefaults")]
    public HprpChrome? ChromeDefaults { get; init; }

    [JsonPropertyName("inspectorFields")]
    public IReadOnlyList<string> InspectorFields { get; init; } = [];

    [JsonPropertyName("labelKeys")]
    public IReadOnlyList<string> LabelKeys { get; init; } = [];

    public bool AllowsBind(string? bind)
    {
        if (string.IsNullOrWhiteSpace(bind))
            return true;

        return BindFields.Any(f => string.Equals(f.Bind, bind.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Studio / validator contract per widget. Dense recipes are C# renderers of this file shape.
/// </summary>
public static class HprpWidgetRecipes
{
    public static readonly HprpWidgetRecipe ClinicalHctEpoAnnualTable = CreateAnnualTable();
    public static readonly HprpWidgetRecipe ClinicalHctEpoCopay = CreateCopay();
    public static readonly HprpWidgetRecipe HemosheetDialysisRecords = CreateDialysisRecords();

    public static readonly IReadOnlyList<HprpWidgetRecipe> Dense = BuildDense();
    public static readonly IReadOnlyList<HprpWidgetRecipe> Blocks = BuildBlocks();

    public static readonly IReadOnlyDictionary<string, HprpWidgetRecipe> ById = Dense
        .Concat(Blocks)
        .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

    public static HprpWidgetRecipe? TryGet(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return ById.TryGetValue(id.Trim(), out var recipe) ? recipe : null;
    }

    private static HprpWidgetRecipe CreateAnnualTable()
    {
        HprpBindField[] binds =
        [
            new() { Bind = "hb", LabelKey = "colHb", DefaultLabel = "Hb(g/dL)" },
            new() { Bind = "hct", LabelKey = "colHct", DefaultLabel = "Hct(%)" },
            new() { Bind = "epoName", LabelKey = "colEpo", DefaultLabel = "EPO" },
            new() { Bind = "frequencyText", LabelKey = "colFrequency", DefaultLabel = "จำนวนเข็ม/Wk" },
            new() { Bind = "injectionDate", LabelKey = "colInjectDay", DefaultLabel = "วันฉีด" },
            new() { Bind = "remarks", LabelKey = "colRemarks", DefaultLabel = "หมายเหตุ" },
        ];

        return new HprpWidgetRecipe
        {
            Id = HprpWidgetIds.ClinicalHctEpoAnnualTable,
            Kind = HprpWidgetRecipe.KindDense,
            Slot = HprpWidgetRecipe.SlotBody,
            AllowedOn = [ClinicalReportCatalog.HctEpo],
            BindFields = binds,
            DefaultColumnPlan =
            [
                new() { Bind = "hb", LabelKey = "colHb", Weight = 1.0f, Center = true, IsLab = true },
                new() { Bind = "hct", LabelKey = "colHct", Weight = 1.0f, Center = true, IsLab = true },
                new() { Bind = "epoName", LabelKey = "colEpo", Weight = 1.8f, Center = false, IsLab = false },
                new() { Bind = "frequencyText", LabelKey = "colFrequency", Weight = 1.8f, Center = false, IsLab = false },
                new() { Bind = "injectionDate", LabelKey = "colInjectDay", Weight = 1.2f, Center = true, IsLab = false },
                new() { Bind = "remarks", LabelKey = "colRemarks", Weight = 1.4f, Center = false, IsLab = false },
            ],
            ChromeDefaults = new HprpChrome
            {
                HeaderFill = HprpChrome.BrandingHeaderFill,
                Border = "thin",
            },
            InspectorFields = ["chrome.headerFill", "chrome.border", "chrome.fontSize", "columnPlan", "when"],
            LabelKeys = ["colDate", "colHb", "colHct", "colEpo", "colFrequency", "colInjectDay", "colRemarks"],
        };
    }

    private static HprpWidgetRecipe CreateCopay() => new()
    {
        Id = HprpWidgetIds.ClinicalHctEpoCopay,
        Kind = HprpWidgetRecipe.KindDense,
        Slot = HprpWidgetRecipe.SlotBody,
        AllowedOn = [ClinicalReportCatalog.HctEpo, ClinicalReportCatalog.EpoDrug],
        ChromeDefaults = new HprpChrome
        {
            HeaderFill = HprpChrome.BrandingHeaderFill,
            Border = "thin",
        },
        InspectorFields = ["chrome.headerFill", "chrome.border", "chrome.fontSize", "when"],
        LabelKeys = ["nhso", "nhsoInjections", "sso", "ssoHctLe36", "ssoHctGt36", "ssoHctGe39"],
    };

    /// <summary>Defaults match thaiur layout (Thai Time/Note); default/rama variants store English in their layout files.</summary>
    private static HprpWidgetRecipe CreateDialysisRecords() => new()
    {
        Id = HprpWidgetIds.HemosheetDialysisRecords,
        Kind = HprpWidgetRecipe.KindDense,
        Slot = HprpWidgetRecipe.SlotSections,
        AllowedOn = [ClinicalReportCatalog.HemodialysisRecord],
        DefaultColumns =
        [
            "เวลา", "BP", "MAP", "Pulse", "EBFR", "AP", "VP", "TMP", "Cond.", "UFR", "Total UF", "หมายเหตุ",
        ],
        DefaultColumnsWhen = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["feature:showHdfColumns"] =
            [
                "เวลา", "BP", "MAP", "Pulse", "EBFR", "AP", "VP",
                "Substitute total", "Substitute rate",
                "TMP", "Cond.", "UFR", "Total UF", "หมายเหตุ",
            ],
        },
        ChromeDefaults = new HprpChrome
        {
            HeaderFill = HprpChrome.BrandingHeaderFill,
        },
        InspectorFields =
        [
            "when", "variant", "chrome.headerFill", "chrome.border", "columns", "columnsWhen", "fixedLinesFrom",
        ],
    };

    private static IReadOnlyList<HprpWidgetRecipe> BuildDense()
    {
        var list = new List<HprpWidgetRecipe>
        {
            new()
            {
                Id = HprpWidgetIds.ThaiUrHeader,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn =
                [
                    ClinicalReportCatalog.HctEpo,
                    ClinicalReportCatalog.EpoDrug,
                    ClinicalReportCatalog.ProgressNote,
                    ClinicalReportCatalog.ConsentTh,
                    ClinicalReportCatalog.ConsentEn,
                ],
                InspectorFields = ["when"],
            },
            ClinicalHctEpoAnnualTable,
            ClinicalHctEpoCopay,
            new()
            {
                Id = HprpWidgetIds.ClinicalEpoDrugTable,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.EpoDrug],
                InspectorFields = ["chrome.headerFill", "chrome.border", "when"],
                LabelKeys = ["month", "yearBe", "epoName", "needlesPerWeek"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalSoapTable,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.ProgressNote],
                InspectorFields =
                [
                    "chrome.headerFill",
                    "chrome.border",
                    "chrome.fontSize",
                    "chrome.rowHeightMm",
                    "chrome.columnWidths",
                    "chrome.bandWeights",
                    "when",
                ],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalChecklistPatient,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.ProgressNoteChecklist],
                InspectorFields = ["when"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalChecklistGrid,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.ProgressNoteChecklist],
                InspectorFields = ["chrome.headerFill", "chrome.border", "chrome.fontSize", "when"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalChecklistTextNotes,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.ProgressNoteChecklist],
                InspectorFields = ["when"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalConsentNarrative,
                Kind = HprpWidgetRecipe.KindDense,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [ClinicalReportCatalog.ConsentTh, ClinicalReportCatalog.ConsentEn],
                InspectorFields = ["when"],
            },
            HemosheetDialysisRecords,
        };

        foreach (var id in HprpWidgetIds.All)
        {
            if (!id.StartsWith("hemosheet.", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(id, HprpWidgetIds.HemosheetDialysisRecords, StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(CreateHemosheetSection(id));
        }

        return list;
    }

    private static HprpWidgetRecipe CreateHemosheetSection(string id)
    {
        var fixedLines = id switch
        {
            HprpWidgetIds.HemosheetNurseRecords
                or HprpWidgetIds.HemosheetDoctorRecords
                or HprpWidgetIds.HemosheetMedicineRecords
                or HprpWidgetIds.HemosheetProgressNotes => true,
            _ => false,
        };

        var fields = new List<string> { "when", "variant" };
        if (fixedLines)
            fields.Add("fixedLinesFrom");
        fields.AddRange(["chrome.headerFill", "chrome.border"]);

        return new HprpWidgetRecipe
        {
            Id = id,
            Kind = HprpWidgetRecipe.KindDense,
            Slot = HprpWidgetRecipe.SlotSections,
            AllowedOn = [ClinicalReportCatalog.HemodialysisRecord],
            InspectorFields = fields,
        };
    }

    private static IReadOnlyList<HprpWidgetRecipe> BuildBlocks() =>
        HprpWidgetIds.BlockTypes
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(type => new HprpWidgetRecipe
            {
                Id = type,
                Kind = HprpWidgetRecipe.KindBlock,
                Slot = HprpWidgetRecipe.SlotBody,
                AllowedOn = [],
                InspectorFields = type switch
                {
                    "key-value-table" => ["title", "rows", "chrome.headerFill", "chrome.border", "chrome.fontSize", "when"],
                    "field-grid" => ["title", "fields", "columns", "chrome.headerFill", "chrome.fontSize", "when"],
                    "data-grid" => ["title", "bindRows", "columnHeaders", "chrome.headerFill", "chrome.fontSize", "when"],
                    "text" => ["title", "content", "bind", "style", "chrome.fontSize", "when"],
                    "signature" => ["title", "when"],
                    "patient-info" => ["when"],
                    "row" => ["gapMm", "when"],
                    "column-stack" => ["when"],
                    _ => ["title", "when"],
                },
            })
            .ToList();
}
