using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Application.Catalog;

public interface IReportCatalogService
{
    IReadOnlyList<ReportCatalogItemDto> GetCatalog(string tenantCode, bool menuOnly = false);
}

public sealed class ReportCatalogService : IReportCatalogService
{
    private const string MedicinePreparationRound = ReportDataFetchRegistry.MedicinePreparationRound;

    private readonly IHprpTemplateStore _store;

    public ReportCatalogService(IHprpTemplateStore store)
    {
        _store = store;
    }

    public IReadOnlyList<ReportCatalogItemDto> GetCatalog(string tenantCode, bool menuOnly = false)
    {
        var byId = new Dictionary<string, ReportCatalogItemDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in _store.ListDefaultManifests())
        {
            var item = BuildItem(tenantCode, manifest);
            byId[item.Id] = item;
        }

        // Standalone reports that may not ship an unpacked .hprp folder yet.
        if (!byId.ContainsKey(MedicinePreparationRound))
        {
            byId[MedicinePreparationRound] = BuildMedicinePrepFallback(tenantCode);
        }

        var items = byId.Values.AsEnumerable();
        if (menuOnly)
            items = items.Where(i => i.VisibleInMenu);

        return items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ReportCatalogItemDto BuildItem(string tenantCode, HprpManifest manifest)
    {
        var ui = manifest.Ui;
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(manifest.Id);
        var fetchKind = ReportDataFetchRegistry.Resolve(engineId, manifest);
        var rendererKind = ResolveRendererKind(engineId);
        var previewMode = ClinicalReportCatalog.UsesDensePdfPreview(engineId) ? "pdf" : "dom";

        return new ReportCatalogItemDto
        {
            Id = engineId,
            DisplayName = string.IsNullOrWhiteSpace(manifest.DisplayName) ? engineId : manifest.DisplayName,
            RequiresSignature = manifest.RequiresSignature,
            DataAdapter = manifest.DataAdapter,
            EngineVersion = manifest.EngineVersion,
            EntryMode = ui?.EntryMode ?? InferEntryMode(engineId),
            MenuGroup = ui?.MenuGroup ?? InferMenuGroup(engineId),
            SortOrder = ui?.SortOrder ?? InferSortOrder(engineId),
            VisibleInMenu = ui?.VisibleInMenu ?? true,
            ReportDataPath = ui?.ReportDataPath
                ?? (fetchKind == ReportDataFetchKind.FormPatientByAdapter
                    || fetchKind == ReportDataFetchKind.Clinical07LabPatient
                    ? ReportDataFetchRegistry.DefaultFormReportDataPath
                    : null),
            Parameters = MapParameters(ui?.Parameters),
            FetchKind = fetchKind.ToString(),
            RendererKind = rendererKind,
            PreviewModeHint = previewMode,
            HasTenantOverride = _store.HasTenantOverride(tenantCode, engineId),
        };
    }

    private ReportCatalogItemDto BuildMedicinePrepFallback(string tenantCode)
    {
        return new ReportCatalogItemDto
        {
            Id = MedicinePreparationRound,
            DisplayName = "Medicine Preparation",
            RequiresSignature = false,
            DataAdapter = HprpDataAdapterIds.MedicinePreparationRound,
            EngineVersion = HprpEngine.CurrentVersion,
            EntryMode = "unitDateRound",
            MenuGroup = "standalone",
            SortOrder = 1000,
            VisibleInMenu = true,
            Parameters =
            [
                new ReportCatalogParameterDto { Name = "unitId", Source = "route", Required = true },
                new ReportCatalogParameterDto { Name = "date", Source = "route", Required = true },
                new ReportCatalogParameterDto { Name = "sectionId", Source = "route", Required = true },
            ],
            FetchKind = ReportDataFetchKind.MedicinePreparationRound.ToString(),
            RendererKind = "dedicated",
            PreviewModeHint = "pdf",
            HasTenantOverride = _store.HasTenantOverride(tenantCode, MedicinePreparationRound),
        };
    }

    private static IReadOnlyList<ReportCatalogParameterDto> MapParameters(
        IList<HprpUiParameterSpec>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return Array.Empty<ReportCatalogParameterDto>();

        return parameters.Select(p => new ReportCatalogParameterDto
        {
            Name = p.Name,
            Source = p.Source,
            RouteKey = p.RouteKey,
            Format = p.Format,
            Generator = p.Generator,
            Months = p.Months,
            Required = p.Required,
            Value = JsonElementToObject(p.Value),
        }).ToList();
    }

    private static object? JsonElementToObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var l) => l,
            JsonValueKind.Number => value.GetDouble(),
            _ => value.GetRawText(),
        };
    }

    private static string ResolveRendererKind(string engineId)
    {
        if (ClinicalReportCatalog.IsHemodialysisRecord(engineId))
            return "hemosheet";

        if (string.Equals(engineId, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.EpoDrug, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.ProgressNote, StringComparison.OrdinalIgnoreCase)
            || ClinicalReportCatalog.IsConsentReport(engineId)
            || string.Equals(engineId, MedicinePreparationRound, StringComparison.OrdinalIgnoreCase))
        {
            return "dedicated";
        }

        return "default";
    }

    private static string InferEntryMode(string engineId)
    {
        if (ClinicalReportCatalog.IsHemodialysisRecord(engineId))
            return "hemosheetList";
        if (string.Equals(engineId, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase))
            return "patientYear";
        if (string.Equals(engineId, ClinicalReportCatalog.EpoDrug, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.ProgressNote, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.MarMonth, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.HdSummary, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, ClinicalReportCatalog.AdequacySummary, StringComparison.OrdinalIgnoreCase))
            return "patientMonth";
        if (string.Equals(engineId, MedicinePreparationRound, StringComparison.OrdinalIgnoreCase))
            return "unitDateRound";
        return "patient";
    }

    private static string InferMenuGroup(string engineId) =>
        string.Equals(engineId, MedicinePreparationRound, StringComparison.OrdinalIgnoreCase)
            ? "standalone"
            : "clinical";

    private static int InferSortOrder(string engineId)
    {
        if (ClinicalReportCatalog.TryGetDefinition(engineId, out _))
        {
            // clinical-NN-... → NN * 10
            var parts = engineId.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[1], out var n))
                return n * 10;
        }

        return 900;
    }
}
