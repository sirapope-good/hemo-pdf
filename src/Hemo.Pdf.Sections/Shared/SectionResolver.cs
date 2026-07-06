using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Sections.Shared;

public sealed class SectionResolver<TSection> : ISectionResolver<TSection>
    where TSection : notnull
{
    public const string Wildcard = "*";

    private readonly IReadOnlyDictionary<(string tenantCode, string templateId), Type> _registry;
    private readonly Type _fallbackType;
    private readonly IServiceProvider _serviceProvider;

    public SectionResolver(
        IEnumerable<(string tenantCode, string templateId, Type implementationType)> items,
        IServiceProvider serviceProvider,
        Type fallbackType)
    {
        _registry = items.ToDictionary(
            x => (x.tenantCode, x.templateId),
            x => x.implementationType,
            new TenantTemplateKeyComparer());
        _serviceProvider = serviceProvider;
        _fallbackType = fallbackType;
    }

    public TSection Resolve(PdfReportContext context)
    {
        var tenantCode = context.TenantCode;
        var templateId = context.ReportTemplateId;

        if (TryResolve(tenantCode, templateId, out var type) ||
            TryResolve(tenantCode, Wildcard, out type) ||
            TryResolve(Wildcard, templateId, out type))
        {
            return (TSection)_serviceProvider.GetRequiredService(type);
        }

        return (TSection)_serviceProvider.GetRequiredService(_fallbackType);
    }

    private bool TryResolve(string tenantCode, string templateId, out Type type)
    {
        return _registry.TryGetValue((tenantCode, templateId), out type!);
    }

    private sealed class TenantTemplateKeyComparer : IEqualityComparer<(string tenantCode, string templateId)>
    {
        public bool Equals((string tenantCode, string templateId) x, (string tenantCode, string templateId) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.tenantCode, y.tenantCode) &&
            StringComparer.OrdinalIgnoreCase.Equals(x.templateId, y.templateId);

        public int GetHashCode((string tenantCode, string templateId) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.tenantCode),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.templateId));
    }
}
