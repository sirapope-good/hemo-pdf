using System.Text.Json.Serialization;

namespace Hemo.Pdf.Application.Catalog;

public sealed class ReportCatalogItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("requiresSignature")]
    public bool RequiresSignature { get; init; }

    [JsonPropertyName("dataAdapter")]
    public string DataAdapter { get; init; } = "";

    [JsonPropertyName("engineVersion")]
    public int EngineVersion { get; init; }

    [JsonPropertyName("entryMode")]
    public string EntryMode { get; init; } = "patient";

    [JsonPropertyName("menuGroup")]
    public string MenuGroup { get; init; } = "clinical";

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("visibleInMenu")]
    public bool VisibleInMenu { get; init; } = true;

    [JsonPropertyName("reportDataPath")]
    public string? ReportDataPath { get; init; }

    [JsonPropertyName("parameters")]
    public IReadOnlyList<ReportCatalogParameterDto> Parameters { get; init; } = Array.Empty<ReportCatalogParameterDto>();

    [JsonPropertyName("fetchKind")]
    public string FetchKind { get; init; } = "";

    [JsonPropertyName("rendererKind")]
    public string RendererKind { get; init; } = "default";

    [JsonPropertyName("previewModeHint")]
    public string PreviewModeHint { get; init; } = "dom";

    [JsonPropertyName("hasTenantOverride")]
    public bool HasTenantOverride { get; init; }
}

public sealed class ReportCatalogParameterDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

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

    [JsonPropertyName("value")]
    public object? Value { get; init; }
}
