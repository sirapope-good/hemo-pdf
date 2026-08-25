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

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = KindDense;

    [JsonPropertyName("allowedOn")]
    public IReadOnlyList<string> AllowedOn { get; init; } = [];

    [JsonPropertyName("bindFields")]
    public IReadOnlyList<HprpBindField> BindFields { get; init; } = [];

    [JsonPropertyName("defaultColumnPlan")]
    public IReadOnlyList<HprpColumnPlanItem> DefaultColumnPlan { get; init; } = [];

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
        AllowedOn = [ClinicalReportCatalog.HctEpo, ClinicalReportCatalog.EpoDrug],
        ChromeDefaults = new HprpChrome
        {
            HeaderFill = HprpChrome.BrandingHeaderFill,
            Border = "thin",
        },
        InspectorFields = ["chrome.headerFill", "chrome.border", "chrome.fontSize", "when"],
        LabelKeys = ["nhso", "nhsoInjections", "sso", "ssoHctLe36", "ssoHctGt36", "ssoHctGe39"],
    };

    private static IReadOnlyList<HprpWidgetRecipe> BuildDense()
    {
        var list = new List<HprpWidgetRecipe>
        {
            new()
            {
                Id = HprpWidgetIds.ThaiUrHeader,
                Kind = HprpWidgetRecipe.KindDense,
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
                AllowedOn = [ClinicalReportCatalog.EpoDrug],
                InspectorFields = ["chrome.headerFill", "chrome.border", "when"],
                LabelKeys = ["month", "yearBe", "epoName", "needlesPerWeek"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalSoapTable,
                Kind = HprpWidgetRecipe.KindDense,
                AllowedOn = [ClinicalReportCatalog.ProgressNote],
                InspectorFields = ["chrome.headerFill", "chrome.border", "when"],
            },
            new()
            {
                Id = HprpWidgetIds.ClinicalConsentNarrative,
                Kind = HprpWidgetRecipe.KindDense,
                AllowedOn = [ClinicalReportCatalog.ConsentTh, ClinicalReportCatalog.ConsentEn],
                InspectorFields = ["when"],
            },
        };

        foreach (var id in HprpWidgetIds.All)
        {
            if (!id.StartsWith("hemosheet.", StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(new HprpWidgetRecipe
            {
                Id = id,
                Kind = HprpWidgetRecipe.KindDense,
                AllowedOn = [ClinicalReportCatalog.HemodialysisRecord],
                InspectorFields = ["chrome.headerFill", "chrome.border", "columns", "when", "variant"],
            });
        }

        return list;
    }

    private static IReadOnlyList<HprpWidgetRecipe> BuildBlocks() =>
        HprpWidgetIds.BlockTypes
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(type => new HprpWidgetRecipe
            {
                Id = type,
                Kind = HprpWidgetRecipe.KindBlock,
                AllowedOn = [],
                InspectorFields = type switch
                {
                    "key-value-table" => ["title", "rows", "chrome.headerFill", "chrome.border", "when"],
                    "field-grid" => ["title", "fields", "columns", "chrome.headerFill", "when"],
                    "data-grid" => ["title", "bindRows", "columnHeaders", "chrome.headerFill", "when"],
                    "text" => ["title", "content", "bind", "style", "when"],
                    "signature" => ["title", "when"],
                    "patient-info" => ["when"],
                    _ => ["title", "when"],
                },
            })
            .ToList();
}
