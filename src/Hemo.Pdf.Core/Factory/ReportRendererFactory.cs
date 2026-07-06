using Hemo.Pdf.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Core.Factory;

public sealed class ReportRendererFactory : IReportRendererFactory
{
    private readonly IReadOnlyDictionary<string, Type> _registry;
    private readonly Type _fallbackType;
    private readonly IServiceProvider _serviceProvider;

    public ReportRendererFactory(
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

    public IReportRenderer Create(string reportTemplateId)
    {
        if (!_registry.TryGetValue(reportTemplateId, out var type))
        {
            type = _fallbackType;
        }

        return (IReportRenderer)_serviceProvider.GetRequiredService(type);
    }
}
