using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetComposer : BaseReportComposer<HemosheetReportViewModel>
{
    private readonly IHemosheetLayoutPlanner _planner;
    private readonly PatientInfoSection _patientInfo = new();
    private readonly KeyValueTableSection _keyValueTable = new();
    private readonly DataGridSection _dataGrid = new();
    private readonly ChecklistTableSection _checklistTable = new();
    private readonly SignatureBlockSection _signatureBlock = new();

    public HemosheetComposer(
        IHemosheetLayoutPlanner planner,
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
        _planner = planner;
    }

    protected override void ComposeContent(
        IContainer container,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            foreach (var plan in _planner.Plan(viewModel))
            {
                col.Item().Element(c => ComposeSection(c, plan, viewModel, context));
            }
        });
    }

    private void ComposeSection(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var features = viewModel.LayoutContext.Features;

        switch (plan.SectionId)
        {
            case HemosheetSectionId.Patient:
                ComposePatient(container, viewModel);
                break;
            case HemosheetSectionId.SessionMeta:
            case HemosheetSectionId.Dehydration:
            case HemosheetSectionId.Prescription:
                ComposeKeyValueSection(container, plan.SectionId, viewModel, features);
                break;
            case HemosheetSectionId.VascularAccess:
                ComposeVascularAccess(container, viewModel, plan.Variant);
                break;
            case HemosheetSectionId.AssessmentPre:
            case HemosheetSectionId.AssessmentRe:
            case HemosheetSectionId.AssessmentPost:
            case HemosheetSectionId.AssessmentOther:
                ComposeAssessment(container, plan.SectionId, viewModel);
                break;
            case HemosheetSectionId.DialysisRecords:
            case HemosheetSectionId.NurseRecords:
            case HemosheetSectionId.DoctorRecords:
            case HemosheetSectionId.MedicineRecords:
            case HemosheetSectionId.ProgressNotes:
                ComposeDataGridSection(container, plan, viewModel);
                break;
            case HemosheetSectionId.NursesInShift:
                ComposeNursesInShift(container, viewModel, features);
                break;
            case HemosheetSectionId.Consent:
                container.Text("ผู้ป่วยให้ความยินยอมในการรักษา").Italic();
                break;
            case HemosheetSectionId.Signatures:
                _signatureBlock.Compose(container, viewModel, context);
                break;
        }
    }

    private void ComposePatient(IContainer container, HemosheetReportViewModel viewModel)
    {
        var source = new HemosheetPatientInfoAdapter(viewModel);
        _patientInfo.Compose(container, source, new PdfReportContext
        {
            ReportTemplateId = "",
            TenantCode = "",
        });
    }

    private void ComposeKeyValueSection(
        IContainer container,
        HemosheetSectionId sectionId,
        HemosheetReportViewModel viewModel,
        IReadOnlyDictionary<string, bool> features)
    {
        var block = sectionId switch
        {
            HemosheetSectionId.SessionMeta => HemosheetPreviewMappers.MapSessionMeta(viewModel),
            HemosheetSectionId.Dehydration => HemosheetPreviewMappers.MapDehydration(viewModel),
            HemosheetSectionId.Prescription => HemosheetPreviewMappers.MapPrescription(viewModel, features),
            _ => null,
        };

        if (block is null)
        {
            return;
        }

        var source = new KeyValueRowsAdapter(block.Title, block.Rows);
        _keyValueTable.Compose(container, source, new PdfReportContext { ReportTemplateId = "", TenantCode = "" });
    }

    private void ComposeVascularAccess(IContainer container, HemosheetReportViewModel viewModel, string? variant)
    {
        var block = HemosheetPreviewMappers.MapVascularAccess(viewModel, variant);
        if (block is null)
        {
            return;
        }

        var source = new KeyValueRowsAdapter(block.Title, block.Rows);
        _keyValueTable.Compose(container, source, new PdfReportContext { ReportTemplateId = "", TenantCode = "" });
    }

    private void ComposeAssessment(IContainer container, HemosheetSectionId sectionId, HemosheetReportViewModel viewModel)
    {
        var (title, items) = sectionId switch
        {
            HemosheetSectionId.AssessmentPre => ("Assessment (Pre)", viewModel.Assessments.Pre),
            HemosheetSectionId.AssessmentRe => ("Assessment (Re)", viewModel.Assessments.Re),
            HemosheetSectionId.AssessmentPost => ("Assessment (Post)", viewModel.Assessments.Post),
            _ => ("Assessment (Other)", viewModel.Assessments.Other),
        };

        var source = new ChecklistAdapter(title, items);
        _checklistTable.Compose(container, source, new PdfReportContext { ReportTemplateId = "", TenantCode = "" });
    }

    private void ComposeDataGridSection(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel)
    {
        DataGridReportBlock? block = plan.SectionId switch
        {
            HemosheetSectionId.DialysisRecords => HemosheetPreviewMappers.MapDialysisRecords(
                viewModel, plan.VisibleColumns, plan.FixedLineCount),
            HemosheetSectionId.NurseRecords => HemosheetPreviewMappers.MapTextRecords(
                "บันทึกพยาบาล", viewModel.NurseRecords, plan.FixedLineCount),
            HemosheetSectionId.DoctorRecords => HemosheetPreviewMappers.MapTextRecords(
                "บันทึกแพทย์", viewModel.DoctorRecords, plan.FixedLineCount),
            HemosheetSectionId.MedicineRecords => HemosheetPreviewMappers.MapMedicineRecords(
                viewModel, plan.FixedLineCount),
            HemosheetSectionId.ProgressNotes => HemosheetPreviewMappers.MapProgressNotes(
                viewModel, plan.FixedLineCount),
            _ => null,
        };

        if (block is null)
        {
            return;
        }

        var source = new DataGridAdapter(block);
        _dataGrid.Compose(container, source, new PdfReportContext { ReportTemplateId = "", TenantCode = "" });
    }

    private void ComposeNursesInShift(
        IContainer container,
        HemosheetReportViewModel viewModel,
        IReadOnlyDictionary<string, bool> features)
    {
        var textBlock = HemosheetPreviewMappers.MapNursesInShift(viewModel, features);
        if (textBlock is null)
        {
            return;
        }

        container.Column(col =>
        {
            col.Item().Text("พยาบาลเวร").SemiBold();
            col.Item().Text(textBlock.Content);
        });
    }

    private sealed class HemosheetPatientInfoAdapter(HemosheetReportViewModel vm) : IPatientInfoSource
    {
        public PatientInfoModel PatientInfo { get; } = new()
        {
            Name = vm.Patient.Name,
            HospitalNumber = vm.Patient.Hn,
            DateOfBirth = vm.Patient.BirthDate?.ToString("yyyy-MM-dd"),
            Gender = vm.Patient.Sex,
            Unit = vm.Unit.FullName,
        };
    }

    private sealed class KeyValueRowsAdapter(string? title, IReadOnlyList<LabelValue> rows) : IKeyValueRowsSource
    {
        public string? SectionTitle => title;
        public IReadOnlyList<KeyValuePair<string, string?>> Rows =>
            rows.Select(r => new KeyValuePair<string, string?>(r.Label, r.Value)).ToList();
    }

    private sealed class ChecklistAdapter(string title, IList<HemosheetAssessmentItemViewModel> items) : IChecklistSource
    {
        public ChecklistTableModel? Checklist { get; } = new()
        {
            Title = title,
            Items = items.Select(i => new ChecklistItem
            {
                Label = i.Name ?? "",
                IsChecked = i.Checked,
                Notes = i.Text,
            }).ToList(),
        };
    }

    private sealed class DataGridAdapter(DataGridReportBlock block) : IDataGridSource
    {
        public DataGridModel? Grid { get; } = new()
        {
            Title = block.Title,
            ColumnHeaders = block.Columns.ToList(),
            Rows = block.Rows.Select(row => row.Select(v => (string?)v).ToList()).ToList(),
        };
    }
}
