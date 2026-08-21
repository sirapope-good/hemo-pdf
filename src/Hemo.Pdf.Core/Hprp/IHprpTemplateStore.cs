using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Hprp;

public interface IHprpTemplateStore
{
    HprpPackage? TryGetCached(string tenantCode, string templateId, string? variant = null);

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

    /// <summary>One manifest per engine template id (default variant).</summary>
    IReadOnlyList<HprpManifest> ListDefaultManifests();

    /// <summary>Layout-profile packages (clinical-03 variants). Filter with <paramref name="role"/>.</summary>
    IReadOnlyList<HprpManifest> ListLayoutProfiles(string? role = null)
    {
        var target = string.IsNullOrWhiteSpace(role)
            ? HprpManifestUi.RoleHemosheetLayoutProfile
            : role.Trim();

        return ListDefaultManifests()
            .Where(m => string.Equals(m.Ui?.Role, target, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    bool HasTenantOverride(string tenantCode, string templateId);

    /// <summary>Force the next lookup to rescan disk (after Studio pack).</summary>
    void Invalidate()
    {
    }

    /// <summary>Every cached variant package (not just default manifests).</summary>
    IReadOnlyList<HprpPackage> ListCachedPackages() => [];
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
