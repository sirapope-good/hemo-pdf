using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Helpers;
using Hemo.Pdf.Sections.Preview;
using Hemo.Pdf.Sections.Preview.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static Hemo.Pdf.Layouts.Hemosheet.HemosheetSectionAdapters;

namespace Hemo.Pdf.Layouts.Hemosheet.Renderers;

internal abstract class HemosheetSectionRendererBase : IHemosheetSectionRenderer
{
    public abstract HemosheetSectionId SectionId { get; }

    public abstract IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context);

    public abstract void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context);

    protected static PdfReportContext EmptyContext => new() { ReportTemplateId = "", TenantCode = "" };

    protected static IReadOnlyList<ReportBlock> Single(ReportBlock? block) =>
        block is null ? [] : [block];
}

internal sealed class PatientSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.Patient;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapPatient(viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        ReportBlockPdfComposer.Compose(container, HemosheetPreviewMappers.MapPatient(viewModel), EmptyContext);
}

internal sealed class SessionMetaSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.SessionMeta;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapSessionMeta(viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = HemosheetPreviewMappers.MapSessionMeta(viewModel);
        DehydrationSectionRenderer.ComposeFieldOrKeyValue(container, block);
    }
}

internal sealed class DehydrationSectionRenderer : HemosheetSectionRendererBase
{
    private readonly KeyValueTableSection _keyValueSection = new();
    private readonly FieldGridSection _fieldGridSection = new();

    public override HemosheetSectionId SectionId => HemosheetSectionId.Dehydration;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapDehydration(viewModel, viewModel.LayoutContext.Features));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = HemosheetPreviewMappers.MapDehydration(viewModel, viewModel.LayoutContext.Features);
        ComposeFieldOrKeyValue(container, block);
    }

    internal static void ComposeFieldOrKeyValue(IContainer container, ReportBlock? block)
    {
        switch (block)
        {
            case FieldGridReportBlock fieldGrid:
                new FieldGridSection().Compose(container, new FieldGridAdapter(fieldGrid), EmptyContext);
                break;
            case KeyValueTableReportBlock keyValue:
                new KeyValueTableSection().Compose(container, new KeyValueRowsAdapter(keyValue.Title, keyValue.Rows), EmptyContext);
                break;
        }
    }
}

internal sealed class PrescriptionSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.Prescription;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapPrescription(viewModel, viewModel.LayoutContext.Features));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = HemosheetPreviewMappers.MapPrescription(viewModel, viewModel.LayoutContext.Features);
        DehydrationSectionRenderer.ComposeFieldOrKeyValue(container, block);
    }
}

internal sealed class VascularAccessSectionRenderer : HemosheetSectionRendererBase
{
    private readonly KeyValueTableSection _section = new();

    public override HemosheetSectionId SectionId => HemosheetSectionId.VascularAccess;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapVascularAccess(viewModel, plan.Variant));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = HemosheetPreviewMappers.MapVascularAccess(viewModel, plan.Variant);
        if (block is null)
        {
            return;
        }

        _section.Compose(container, new KeyValueRowsAdapter(block.Title, block.Rows), EmptyContext);
    }
}

internal sealed class AssessmentSectionRenderer : HemosheetSectionRendererBase
{
    private readonly ChecklistTableSection _section = new();
    private readonly HemosheetSectionId _sectionId;
    private readonly string _title;
    private readonly Func<HemosheetReportViewModel, IList<HemosheetAssessmentItemViewModel>> _itemsSelector;

    public AssessmentSectionRenderer(
        HemosheetSectionId sectionId,
        string title,
        Func<HemosheetReportViewModel, IList<HemosheetAssessmentItemViewModel>> itemsSelector)
    {
        _sectionId = sectionId;
        _title = title;
        _itemsSelector = itemsSelector;
    }

    public override HemosheetSectionId SectionId => _sectionId;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapAssessment(_title, _itemsSelector(viewModel)));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        ReportBlockPdfComposer.Compose(
            container,
            HemosheetPreviewMappers.MapAssessment(_title, _itemsSelector(viewModel)),
            EmptyContext);
}

internal sealed class AssessmentPreReSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.AssessmentPreRe;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapPreReAssessmentMatrix(viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        ReportBlockPdfComposer.Compose(
            container,
            HemosheetPreviewMappers.MapPreReAssessmentMatrix(viewModel),
            EmptyContext);
}

internal sealed class DialysisRecordsSectionRenderer : DataGridSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.DialysisRecords;

    protected override DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel) =>
        HemosheetPreviewMappers.MapDialysisRecords(viewModel, plan.VisibleColumns, plan.FixedLineCount);
}

internal sealed class NurseRecordsSectionRenderer : DataGridSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.NurseRecords;

    protected override DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel) =>
        HemosheetPreviewMappers.MapTextRecords("บันทึกพยาบาล", viewModel.NurseRecords, plan.FixedLineCount);
}

internal sealed class DoctorRecordsSectionRenderer : DataGridSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.DoctorRecords;

    protected override DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel) =>
        HemosheetPreviewMappers.MapTextRecords("บันทึกแพทย์", viewModel.DoctorRecords, plan.FixedLineCount);
}

internal sealed class MedicineRecordsSectionRenderer : DataGridSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.MedicineRecords;

    protected override DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel) =>
        HemosheetPreviewMappers.MapMedicineRecords(viewModel, plan.FixedLineCount);
}

internal sealed class ProgressNotesSectionRenderer : DataGridSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.ProgressNotes;

    protected override DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel) =>
        HemosheetPreviewMappers.MapProgressNotes(viewModel, plan.FixedLineCount);
}

internal abstract class DataGridSectionRendererBase : HemosheetSectionRendererBase
{
    private readonly DataGridSection _section = new();

    protected abstract DataGridReportBlock? MapGrid(HemosheetSectionPlan plan, HemosheetReportViewModel viewModel);

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(MapGrid(plan, viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = MapGrid(plan, viewModel);
        if (block is null)
        {
            return;
        }

        _section.Compose(container, new DataGridAdapter(block), EmptyContext);
    }
}

internal sealed class NursesInShiftSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.NursesInShift;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var textBlock = HemosheetPreviewMappers.MapNursesInShift(viewModel, viewModel.LayoutContext.Features);
        if (textBlock is null)
        {
            return [];
        }

        return
        [
            new TextReportBlock
            {
                Title = "พยาบาลเวร",
                Content = textBlock.Content,
                Style = textBlock.Style,
            },
        ];
    }

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var textBlock = HemosheetPreviewMappers.MapNursesInShift(viewModel, viewModel.LayoutContext.Features);
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
}

internal sealed class ConsentSectionRenderer : HemosheetSectionRendererBase
{
    public override HemosheetSectionId SectionId => HemosheetSectionId.Consent;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapConsent(viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        if (!viewModel.IsConsent)
        {
            return;
        }

        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("ผู้ป่วยให้ความยินยอมในการรักษา").Italic();

            var signatureBytes = PdfImageHelpers.LoadLogoFromDataUrl(viewModel.DoctorSignatureBase64);
            if (signatureBytes is { Length: > 0 })
            {
                col.Item().Height(36).Image(signatureBytes).FitHeight();
            }
        });
    }
}

internal sealed class LabsSectionRenderer : HemosheetSectionRendererBase
{
    private readonly KeyValueTableSection _section = new();

    public override HemosheetSectionId SectionId => HemosheetSectionId.Labs;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(HemosheetPreviewMappers.MapLabs(viewModel));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        var block = HemosheetPreviewMappers.MapLabs(viewModel);
        if (block is null)
        {
            return;
        }

        _section.Compose(container, new KeyValueRowsAdapter(block.Title, block.Rows), EmptyContext);
    }
}

internal sealed class SignaturesSectionRenderer : HemosheetSectionRendererBase
{
    private readonly SignatureBlockSection _section = new();

    public override HemosheetSectionId SectionId => HemosheetSectionId.Signatures;

    public override IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        Single(SignaturePreviewMapper.Map(context));

    public override void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context) =>
        _section.Compose(container, viewModel, context);
}
