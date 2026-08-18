using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Generic;
using Hemo.Pdf.Layouts.MedicinePrep;
using Hemo.Pdf.Layouts.Placeholder;
using Hemo.Pdf.Layouts.Preview.Generic;
using Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Layouts;

public static class TemplateRegistration
{
    public static Type FallbackRendererType => typeof(PlaceholderReportRenderer);

    /// <summary>Clinical pack only (<c>clinical-01</c>…<c>16</c>).</summary>
    public static IReadOnlyList<string> AllTemplateIds { get; } =
        ClinicalReportCatalog.All.Select(t => t.Id).ToList();

    public static IServiceCollection AddTemplateServices(this IServiceCollection services)
    {
        services.AddScoped<PlaceholderDataProvider>();
        services.AddScoped<PlaceholderComposer>();
        services.AddScoped<PlaceholderReportRenderer>();

        services.AddScoped<GenericTemplateDataProvider>();
        services.AddScoped<GenericTemplateComposer>();
        services.AddScoped<GenericTemplateReportRenderer>();

        services.AddScoped<ClinicalDefaultDataProvider>();
        services.AddScoped<ClinicalDefaultComposer>();
        services.AddScoped<ClinicalDefaultReportRenderer>();

        services.AddScoped<Clinical01HctEpoDataProvider>();
        services.AddScoped<Clinical01HctEpoComposer>();
        services.AddScoped<Clinical01HctEpoReportRenderer>();

        services.AddScoped<Clinical02EpoDrugDataProvider>();
        services.AddScoped<Clinical02EpoDrugComposer>();
        services.AddScoped<Clinical02EpoDrugReportRenderer>();

        services.AddScoped<Clinical05ProgressNoteDataProvider>();
        services.AddScoped<Clinical05ProgressNoteComposer>();
        services.AddScoped<Clinical05ProgressNoteReportRenderer>();

        services.AddScoped<ConsentReportDataProvider>();
        services.AddScoped<ConsentReportComposer>();
        services.AddScoped<ConsentReportRenderer>();

        services.AddScoped<MedicinePreparationRoundDataProvider>();
        services.AddScoped<MedicinePreparationRoundComposer>();
        services.AddScoped<MedicinePreparationRoundReportRenderer>();

        services.AddScoped<GenericReportDocumentComposer>();
        services.AddScoped<GenericReportPreviewRenderer>();

        services.AddScoped<Hemosheet.HemosheetLayoutPlanner>();
        services.AddScoped<Hemosheet.IHemosheetLayoutPlanner>(sp => sp.GetRequiredService<Hemosheet.HemosheetLayoutPlanner>());
        services.AddHemosheetSectionRenderers();
        services.AddScoped<HemosheetDataProvider>();
        services.AddScoped<HemosheetComposer>();
        services.AddScoped<HemosheetReportRenderer>();
        services.AddScoped<HemosheetReportDocumentComposer>();
        services.AddScoped<HemosheetReportPreviewRenderer>();

        return services;
    }

    public static IEnumerable<(string reportTemplateId, Type rendererType)> GetRendererRegistrations() =>
        TemplateReportRendererFactory.CreateRegistrations();
}
