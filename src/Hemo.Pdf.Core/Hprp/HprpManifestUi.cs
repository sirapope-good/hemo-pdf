using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// FE-facing metadata in <c>manifest.json</c> so the Reports menu / picker
/// can stay generic (no hard-coded clinical-01…16 map on the client).
/// </summary>
public sealed class HprpManifestUi
{
    public static readonly IReadOnlySet<string> EntryModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "hemosheetList",
        "patient",
        "patientMonth",
        "patientYear",
        "unitDateRound",
    };

    public static readonly IReadOnlySet<string> ParameterSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "route",
        "query",
        "constant",
        "default",
    };

    public static readonly IReadOnlySet<string> Generators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "today",
        "lastMonths",
    };

    [JsonPropertyName("entryMode")]
    public string EntryMode { get; init; } = "patient";

    [JsonPropertyName("menuGroup")]
    public string MenuGroup { get; init; } = "clinical";

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("visibleInMenu")]
    public bool VisibleInMenu { get; init; } = true;

    /// <summary>
    /// Optional Web.Api path template for convention fetch, e.g.
    /// <c>api/Patients/{patientId}/reports/{templateId}/report-data</c>.
    /// </summary>
    [JsonPropertyName("reportDataPath")]
    public string? ReportDataPath { get; init; }

    public IList<HprpUiParameterSpec> Parameters { get; init; } = new List<HprpUiParameterSpec>();
}

public sealed class HprpUiParameterSpec
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    /// <summary><c>route</c> | <c>query</c> | <c>constant</c> | <c>default</c></summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = "route";

    [JsonPropertyName("routeKey")]
    public string? RouteKey { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("generator")]
    public string? Generator { get; init; }

    [JsonPropertyName("months")]
    public int? Months { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>Constant value when <see cref="Source"/> is <c>constant</c>.</summary>
    [JsonPropertyName("value")]
    public JsonElement Value { get; init; }
}
