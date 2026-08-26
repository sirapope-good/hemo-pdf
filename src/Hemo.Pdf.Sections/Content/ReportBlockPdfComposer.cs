using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Preview;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public static class ReportBlockPdfComposer
{
  public static void Compose(IContainer container, ReportBlock? block, PdfReportContext context)
  {
    if (block is null)
    {
      return;
    }

    HprpBoxComposer.Apply(container, block.Box, inner => ComposeInner(inner, block, context));
  }

  private static void ComposeInner(IContainer container, ReportBlock block, PdfReportContext context)
  {
    switch (block)
    {
      case PatientInfoReportBlock patientInfo:
        PatientInfoSection.ComposeBlock(container, patientInfo, context);
        break;
      case FieldGridReportBlock fieldGrid:
        new FieldGridSection().Compose(container, new ReportBlockAdapters.FieldGridAdapter(fieldGrid), context);
        break;
      case KeyValueTableReportBlock keyValue:
        new KeyValueTableSection().Compose(container, new ReportBlockAdapters.KeyValueRowsAdapter(keyValue.Title, keyValue.Rows, keyValue.Chrome), context);
        break;
      case DataGridReportBlock dataGrid:
        new DataGridSection().Compose(container, new ReportBlockAdapters.DataGridAdapter(dataGrid), context);
        break;
      case ChecklistTableReportBlock checklist:
        new ChecklistTableSection().Compose(container, checklist, context);
        break;
      case SubHeaderBarReportBlock subHeader:
        new SubHeaderBarSection().Compose(container, subHeader, context);
        break;
      case SectionRowReportBlock sectionRow:
        new SectionRowSection().Compose(container, sectionRow, context);
        break;
      case ColumnStackReportBlock columnStack:
        new SectionRowSection().Compose(container, new SectionRowReportBlock
        {
            Columns = 1,
            Blocks = [columnStack],
        }, context);
        break;
      case ChecklistClusterReportBlock cluster:
        new ChecklistClusterSection().Compose(container, cluster, context);
        break;
      case PrePostHdNotesReportBlock notes:
        new PrePostHdNotesSection().Compose(container, notes, context);
        break;
      case VascularAccessReportBlock vascular:
        new KeyValueTableSection().Compose(container, new ReportBlockAdapters.KeyValueRowsAdapter(vascular.Title, vascular.Rows), context);
        break;
      case TextReportBlock text:
        ComposeText(container, text, context);
        break;
      case SignatureReportBlock:
        new SignatureBlockSection().Compose(container, block, context);
        break;
    }
  }

  private static void ComposeText(IContainer container, TextReportBlock text, PdfReportContext context)
  {
    var style = (text.Style ?? "body").Trim().ToLowerInvariant();
    var fallback = style switch
    {
      "title" => PdfStyleDefaults.Body.SectionTitleFontSize,
      "subtitle" => 9f,
      _ => context.DefaultFontSize ?? PdfStyleDefaults.Body.BaseFontSize,
    };
    var fontSize = HprpChrome.ResolveFontSize(text.Chrome, fallback);

    container.Column(col =>
    {
      if (!string.IsNullOrWhiteSpace(text.Title) && style is not "title" and not "subtitle")
      {
        col.Item().Text(text.Title)
          .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
          .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
          .SemiBold();
      }

      if (string.IsNullOrWhiteSpace(text.Content))
        return;

      var item = col.Item().Text(text.Content)
        .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily);

      if (style == "title")
        item.FontSize(fontSize).SemiBold();
      else
        item.FontSize(fontSize);
    });
  }
}
