using Hemo.Pdf.Application.Guards;
using Hemo.Pdf.Application.Mock;
using Hemo.Pdf.Branding;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Factory;
using Hemo.Pdf.Layouts;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Footers;
using Hemo.Pdf.Sections.Headers;
using Hemo.Pdf.Sections.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHemoPdf(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(HemoPdfOptions.SectionName);
        services.Configure<HemoPdfOptions>(opts => section.Bind(opts));

        var options = section.Get<HemoPdfOptions>() ?? new HemoPdfOptions();

        services.Configure<BrandingOptions>(branding =>
        {
            branding.RootPath = options.BrandingRootPath;
        });

        services.AddSingleton<IPdfRenderer, QuestPdfRenderer>();
        services.AddScoped<IPdfGenerationService, PdfGenerationService>();
        services.AddScoped<IReportPreviewService, ReportPreviewService>();
        services.AddScoped<IPdfGenerationGuard, SignatureRequiredGuard>();

        services.AddScoped<MockTenantContextAccessor>();
        services.AddScoped<ITenantContextAccessor>(sp => sp.GetRequiredService<MockTenantContextAccessor>());

        services.AddSingleton<IBrandingStore, JsonFileBrandingStore>();
        services.AddScoped<IBrandingResolver, BrandingResolver>();

        if (options.UseMockServices)
        {
            services.AddScoped<ISignatureStore, MockSignatureStore>();
        }
        else
        {
            services.AddScoped<ISignatureStore, HemoproSignatureStore>();
        }

        services.AddScoped<ConfigurableHeaderSection>();
        services.AddScoped<HemosheetHeaderSection>();
        services.AddScoped<ConfigurableFooterSection>();
        services.AddScoped<HemosheetFooterSection>();

        services.AddScoped<ISectionResolver<IReportHeaderSection>>(sp =>
            new SectionResolver<IReportHeaderSection>(
                [
                    ("*", ReportTemplates.Hemosheet, typeof(HemosheetHeaderSection)),
                ],
                sp,
                typeof(ConfigurableHeaderSection)));

        services.AddScoped<ISectionResolver<IReportFooterSection>>(sp =>
            new SectionResolver<IReportFooterSection>(
                [
                    ("*", ReportTemplates.Hemosheet, typeof(HemosheetFooterSection)),
                ],
                sp,
                typeof(ConfigurableFooterSection)));

        services.AddTemplateServices();

        services.AddScoped<IReportRendererFactory>(sp =>
            new ReportRendererFactory(
                TemplateRegistration.GetRendererRegistrations(),
                sp,
                TemplateRegistration.FallbackRendererType));

        services.AddScoped<IReportPreviewRendererFactory>(sp =>
            new ReportPreviewRendererFactory(
                TemplateReportPreviewRendererFactory.CreateRegistrations(),
                sp,
                typeof(Layouts.Preview.Generic.GenericReportPreviewRenderer)));

        return services;
    }
}
