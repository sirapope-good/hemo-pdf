using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Preview.Base;
using Hemo.Pdf.Sections.Preview;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;

public sealed class HemosheetReportDocumentComposer : BaseReportDocumentComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;

    public HemosheetReportDocumentComposer(IHemosheetLayoutPlanner planner)
    {
        _planner = planner;
    }

    protected override IReadOnlyList<ReportBlock> ComposeContentBlocks(
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var blocks = new List<ReportBlock>();
        var features = viewModel.LayoutContext.Features;

        foreach (var plan in _planner.Plan(viewModel))
        {
            switch (plan.SectionId)
            {
                case HemosheetSectionId.Patient:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapPatient(viewModel));
                    break;
                case HemosheetSectionId.SessionMeta:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapSessionMeta(viewModel));
                    break;
                case HemosheetSectionId.Dehydration:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapDehydration(viewModel));
                    break;
                case HemosheetSectionId.Prescription:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapPrescription(viewModel, features));
                    break;
                case HemosheetSectionId.VascularAccess:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapVascularAccess(viewModel, plan.Variant));
                    break;
                case HemosheetSectionId.AssessmentPre:
                    AddAssessment(blocks, "Assessment (Pre)", viewModel.Assessments.Pre);
                    break;
                case HemosheetSectionId.AssessmentRe:
                    AddAssessment(blocks, "Assessment (Re)", viewModel.Assessments.Re);
                    break;
                case HemosheetSectionId.AssessmentPost:
                    AddAssessment(blocks, "Assessment (Post)", viewModel.Assessments.Post);
                    break;
                case HemosheetSectionId.AssessmentOther:
                    AddAssessment(blocks, "Assessment (Other)", viewModel.Assessments.Other);
                    break;
                case HemosheetSectionId.DialysisRecords:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapDialysisRecords(
                        viewModel, plan.VisibleColumns, plan.FixedLineCount));
                    break;
                case HemosheetSectionId.NurseRecords:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapTextRecords(
                        "บันทึกพยาบาล", viewModel.NurseRecords, plan.FixedLineCount));
                    break;
                case HemosheetSectionId.DoctorRecords:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapTextRecords(
                        "บันทึกแพทย์", viewModel.DoctorRecords, plan.FixedLineCount));
                    break;
                case HemosheetSectionId.MedicineRecords:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapMedicineRecords(viewModel, plan.FixedLineCount));
                    break;
                case HemosheetSectionId.ProgressNotes:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapProgressNotes(viewModel, plan.FixedLineCount));
                    break;
                case HemosheetSectionId.NursesInShift:
                    blocks.Add(new TextReportBlock
                    {
                        Content = "พยาบาลเวร",
                        Style = "title",
                    });
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapNursesInShift(viewModel, features));
                    break;
                case HemosheetSectionId.Consent:
                    AddIfNotNull(blocks, HemosheetPreviewMappers.MapConsent(viewModel));
                    break;
                case HemosheetSectionId.Signatures:
                    AddIfNotNull(blocks, SignaturePreviewMapper.Map(context));
                    break;
            }
        }

        return blocks;
    }

    private static void AddAssessment(
        List<ReportBlock> blocks,
        string title,
        IList<HemosheetAssessmentItemViewModel> items)
    {
        blocks.Add(new TextReportBlock { Content = title, Style = "title" });
        AddIfNotNull(blocks, HemosheetPreviewMappers.MapAssessment(title, items));
    }

    private static void AddIfNotNull(List<ReportBlock> blocks, ReportBlock? block)
    {
        if (block is not null)
        {
            blocks.Add(block);
        }
    }
}
