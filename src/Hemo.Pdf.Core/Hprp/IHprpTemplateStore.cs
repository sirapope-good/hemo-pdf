using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Hprp;

public interface IHprpTemplateStore
{
    HprpPackage? TryGetCached(string tenantCode, string templateId);

    Task<HprpPackage?> GetAsync(string tenantCode, string templateId, CancellationToken cancellationToken = default);

    Task SaveTenantOverrideAsync(
        string tenantCode,
        string templateId,
        Stream zipStream,
        CancellationToken cancellationToken = default);

    Task DeleteTenantOverrideAsync(
        string tenantCode,
        string templateId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<HprpManifest> ListDefaultManifests();

    bool HasTenantOverride(string tenantCode, string templateId);
}

public static class HprpCatalog
{
    public static ReportTemplateDefinition ToDefinition(HprpManifest manifest) =>
        new()
        {
            Id = manifest.Id,
            DisplayName = manifest.DisplayName,
            RequiresSignature = manifest.RequiresSignature,
        };

    public static bool TryGetDefinition(
        IHprpTemplateStore? store,
        string tenantCode,
        string templateId,
        out ReportTemplateDefinition? definition)
    {
        var package = store?.TryGetCached(tenantCode, templateId);
        if (package is not null)
        {
            definition = ToDefinition(package.Manifest);
            return true;
        }

        definition = null;
        return false;
    }
}
