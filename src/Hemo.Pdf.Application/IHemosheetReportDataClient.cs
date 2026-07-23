using System.Text.Json;

namespace Hemo.Pdf.Application;

public interface IHemosheetReportDataClient
{
    Task<JsonElement> GetRecordReportDataAsync(
        string hemoId,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetTemplateReportDataAsync(
        int unitId,
        string templateMode,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);
}
