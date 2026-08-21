using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hemosheet.Renderers;

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
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapPatient(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class SubHeaderBarSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.SubHeaderBar;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapSubHeaderBar(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapSubHeaderBar(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class PredialysisSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.Predialysis;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapTopLayoutRow(viewModel, viewModel.LayoutContext.Features));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapTopLayoutRow(viewModel, viewModel.LayoutContext.Features);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class UfSummarySectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.UfSummary;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapUfSummary(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapUfSummary(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class NursingCarePlanSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.NursingCarePlan;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapNursingCarePlan(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapNursingCarePlan(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class FooterChecklistsSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.FooterChecklists;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapFooterChecklists(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapFooterChecklists(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class PrePostHdNotesSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.PrePostHdNotes;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapPrePostHdNotes(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapPrePostHdNotes(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class PostVitalsSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.PostVitals;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapPostVitals(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapPostVitals(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}

internal sealed class AvfAssessmentSectionRenderer : HemosheetSectionRendererBase
{
  public override HemosheetSectionId SectionId => HemosheetSectionId.AvfAssessment;

  public override IReadOnlyList<ReportBlock> MapToPreview(
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context) =>
      Single(HemosheetPreviewMappers.MapAvfAssessment(viewModel));

  public override void ComposePdf(
      IContainer container,
      HemosheetSectionPlan plan,
      HemosheetReportViewModel viewModel,
      PdfReportContext context)
  {
    var block = HemosheetPreviewMappers.MapAvfAssessment(viewModel);
    ReportBlockPdfComposer.Compose(container, block, context);
  }
}
