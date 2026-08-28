using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

/// <summary>Reusable table structure (columns, row mode, chrome). Bindings live on layout elements.</summary>
public sealed class HprpTablePreset
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>Optional filters e.g. <c>tenant:hogwarts</c>, <c>clinical</c>.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary><see cref="HprpTableRowModes"/></summary>
    [JsonPropertyName("rowMode")]
    public string RowMode { get; init; } = HprpTableRowModes.Annual;

    [JsonPropertyName("groupCount")]
    public int GroupCount { get; init; } = 12;

    [JsonPropertyName("slotsPerGroup")]
    public int SlotsPerGroup { get; init; } = 3;

    [JsonPropertyName("freedomRowCount")]
    public int FreedomRowCount { get; init; } = 10;

    [JsonPropertyName("dateColumns")]
    public HprpTableDateColumns? DateColumns { get; init; }

    [JsonPropertyName("columns")]
    public IReadOnlyList<HprpTableColumnDef> Columns { get; init; } = [];

    /// <summary>
    /// Hardcoded body rows for <c>freedom</c> mode (e.g. co-pay reference tables).
    /// Each inner list matches <see cref="Columns"/> order. When set, overrides bindings.
    /// </summary>
    [JsonPropertyName("staticRows")]
    public IReadOnlyList<IReadOnlyList<string>>? StaticRows { get; init; }

    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }
}

public sealed class HprpTableDateColumns
{
    [JsonPropertyName("monthWeight")]
    public float MonthWeight { get; init; } = 0.45f;

    [JsonPropertyName("dayWeight")]
    public float DayWeight { get; init; } = 1.35f;

    [JsonPropertyName("dateHeaderLabelKey")]
    public string? DateHeaderLabelKey { get; init; }
}

public sealed class HprpTableColumnDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("labelKey")]
    public string? LabelKey { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("weight")]
    public float Weight { get; init; } = 1f;

    [JsonPropertyName("center")]
    public bool Center { get; init; }

    [JsonPropertyName("isLab")]
    public bool IsLab { get; init; }
}
