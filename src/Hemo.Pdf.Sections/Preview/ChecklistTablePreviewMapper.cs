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

        if (string.Equals(model.Layout, "yn-columns", StringComparison.Ordinal))
        {
            return new ChecklistTableReportBlock
            {
                Title = model.Title,
                Layout = "yn-columns",
                Columns = ["Y", "N", "รายการ", "หมายเหตุ"],
                Rows = model.Items.Select(item => (IReadOnlyList<ChecklistCellValue>)
                [
                    new ChecklistCheckboxCell { Checked = item.IsChecked },
                    new ChecklistCheckboxCell { Checked = !item.IsChecked },
                    new ChecklistTextCell { Text = item.Label },
                    new ChecklistTextCell { Text = string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes! },
                ]).ToList(),
            };
        }

        return new ChecklistTableReportBlock
        {
            Title = model.Title,
            Layout = "default",
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
