using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Generic;
using Hemo.Pdf.Layouts.Placeholder;
using Hemo.Pdf.Layouts.Template01_DialysisSession;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Layouts;

public static class TemplateRegistration
{
    public static Type FallbackRendererType => typeof(PlaceholderReportRenderer);

    public static IReadOnlyList<string> AllTemplateIds { get; } =
        ReportTemplates.All.Select(template => template.Id).ToList();

    public static IServiceCollection AddTemplateServices(this IServiceCollection services)
    {
        services.AddScoped<PlaceholderDataProvider>();
        services.AddScoped<PlaceholderComposer>();
        services.AddScoped<PlaceholderReportRenderer>();

        services.AddScoped<GenericTemplateDataProvider>();
        services.AddScoped<GenericTemplateComposer>();
        services.AddScoped<GenericTemplateReportRenderer>();

        services.AddScoped<DialysisSessionDataProvider>();
        services.AddScoped<DialysisSessionComposer>();
        services.AddScoped<DialysisSessionReportRenderer>();

        return services;
    }

    public static IEnumerable<(string reportTemplateId, Type rendererType)> GetRendererRegistrations() =>
        TemplateReportRendererFactory.CreateRegistrations();
}
