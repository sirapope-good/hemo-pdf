using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Preview;

public static class ChecklistTablePreviewMapper
{
    public static ChecklistTableReportBlock? Map(ChecklistTableModel? model)
    {
        if (model is null || model.Items.Count == 0)
        {
            return null;
        }

        return new ChecklistTableReportBlock
        {
            Title = model.Title,
            Columns = ["", "รายการ", "หมายเหตุ"],
            Rows = model.Items.Select(item => (IReadOnlyList<ChecklistCellValue>)
            [
                new ChecklistCheckboxCell { Checked = item.IsChecked },
                new ChecklistTextCell { Text = item.Label },
                new ChecklistTextCell { Text = string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes! },
            ]).ToList(),
        };
    }
}
