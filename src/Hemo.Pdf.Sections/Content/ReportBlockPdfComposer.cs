using Hemo.Pdf.Core.Context;
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

    switch (block)
    {
      case FieldGridReportBlock fieldGrid:
        new FieldGridSection().Compose(container, new ReportBlockAdapters.FieldGridAdapter(fieldGrid), context);
        break;
      case KeyValueTableReportBlock keyValue:
        new KeyValueTableSection().Compose(container, new ReportBlockAdapters.KeyValueRowsAdapter(keyValue.Title, keyValue.Rows), context);
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
    }
  }
}
