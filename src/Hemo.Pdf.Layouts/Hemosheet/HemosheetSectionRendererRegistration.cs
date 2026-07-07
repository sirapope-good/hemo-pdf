using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hemosheet.Renderers;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Layouts.Hemosheet;

public static class HemosheetSectionRendererRegistration
{
    public static IServiceCollection AddHemosheetSectionRenderers(this IServiceCollection services)
    {
        services.AddSingleton<HemosheetLayoutProfileRegistry>();

        services.AddSingleton<IHemosheetSectionRenderer, PatientSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, SessionMetaSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, DehydrationSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, PrescriptionSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, VascularAccessSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentPre,
            "Assessment (Pre)",
            vm => vm.Assessments.Pre));
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentRe,
            "Assessment (Re)",
            vm => vm.Assessments.Re));
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentPost,
            "Assessment (Post)",
            vm => vm.Assessments.Post));
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentOther,
            "Assessment (Other)",
            vm => vm.Assessments.Other));
        services.AddSingleton<IHemosheetSectionRenderer, LabsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, DialysisRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, NurseRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, DoctorRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, MedicineRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, ProgressNotesSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, NursesInShiftSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, ConsentSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, SignaturesSectionRenderer>();

        services.AddSingleton<HemosheetSectionRendererRegistry>();

        return services;
    }
}
