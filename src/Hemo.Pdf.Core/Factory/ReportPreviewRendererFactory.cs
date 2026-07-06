using Hemo.Pdf.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Core.Factory;

public sealed class ReportPreviewRendererFactory : IReportPreviewRendererFactory
{
    private readonly IReadOnlyDictionary<string, Type> _registry;
    private readonly Type _fallbackType;
    private readonly IServiceProvider _serviceProvider;

    public ReportPreviewRendererFactory(
        IEnumerable<(string reportTemplateId, Type rendererType)> items,
        IServiceProvider serviceProvider,
        Type fallbackType)
    {
        _registry = items.ToDictionary(
            x => x.reportTemplateId,
            x => x.rendererType,
            StringComparer.OrdinalIgnoreCase);
        _serviceProvider = serviceProvider;
        _fallbackType = fallbackType;
    }

    public IReportPreviewRenderer Create(string reportTemplateId)
    {
        if (!_registry.TryGetValue(reportTemplateId, out var type))
        {
            type = _fallbackType;
        }

        return (IReportPreviewRenderer)_serviceProvider.GetRequiredService(type);
    }
}
