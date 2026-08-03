using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hemosheet.Renderers;
using Hemo.Pdf.Sections.Preview.Hemosheet;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Pdf.Layouts.Hemosheet;

public static class HemosheetSectionRendererRegistration
{
    public static IServiceCollection AddHemosheetSectionRenderers(this IServiceCollection services)
    {
        services.AddSingleton<HemosheetLayoutProfileRegistry>();

        services.AddSingleton<IHemosheetSectionRenderer, PatientSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, SubHeaderBarSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, SessionMetaSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, TopLayoutSectionRenderer>();
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
        services.AddSingleton<IHemosheetSectionRenderer, AssessmentPreReSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentPost,
            "Assessment (Post)",
            vm => HemosheetPreviewMappers.SelectPostBodyItems(vm.Assessments.Post)));
        services.AddSingleton<IHemosheetSectionRenderer>(_ => new AssessmentSectionRenderer(
            HemosheetSectionId.AssessmentOther,
            "Assessment (Other)",
            vm => HemosheetPreviewMappers.SelectOtherBodyItems(vm.Assessments.Other)));
        services.AddSingleton<IHemosheetSectionRenderer, LabsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, DialysisRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, UfSummarySectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, NurseRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, DoctorRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, MedicineRecordsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, ProgressNotesSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, NursingCarePlanSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, FooterChecklistsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, PrePostHdNotesSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, PostVitalsSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, AvfAssessmentSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, NursesInShiftSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, ConsentSectionRenderer>();
        services.AddSingleton<IHemosheetSectionRenderer, SignaturesSectionRenderer>();

        services.AddSingleton<HemosheetSectionRendererRegistry>();

        return services;
    }
}
