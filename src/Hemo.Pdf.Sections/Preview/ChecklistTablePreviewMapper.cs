using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Preview;

public static class ChecklistTablePreviewMapper
{
    public const string LayoutDefault = "default";
    public const string LayoutYnColumns = "yn-columns";
    public const string LayoutPreReMatrix = "pre-re-matrix";

    public static ChecklistTableReportBlock? Map(ChecklistTableModel? model)
    {
        if (model is null || model.Items.Count == 0)
        {
            return null;
        }

        if (string.Equals(model.Layout, LayoutYnColumns, StringComparison.Ordinal))
        {
            return new ChecklistTableReportBlock
            {
                Title = model.Title,
                Layout = LayoutYnColumns,
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
            Layout = LayoutDefault,
            Columns = ["", "รายการ", "หมายเหตุ"],
            Rows = model.Items.Select(item => (IReadOnlyList<ChecklistCellValue>)
            [
                new ChecklistCheckboxCell { Checked = item.IsChecked },
                new ChecklistTextCell { Text = item.Label },
                new ChecklistTextCell { Text = string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes! },
            ]).ToList(),
        };
    }

    /// <summary>
    /// Topic | Pre Y/N | Re Y/N | Notes — missing Pre/Re leaves both Y and N unchecked (Telerik parity).
    /// </summary>
    public static ChecklistTableReportBlock? MapPreReMatrix(
        string? title,
        IReadOnlyList<(string Topic, bool? PreChecked, bool? ReChecked, string? Notes)> rows)
    {
        if (rows.Count == 0)
        {
            return null;
        }

        return new ChecklistTableReportBlock
        {
            Title = title,
            Layout = LayoutPreReMatrix,
            Columns = ["Topic", "Pre Y", "Pre N", "Re Y", "Re N", "หมายเหตุ"],
            Rows = rows.Select(r => (IReadOnlyList<ChecklistCellValue>)
            [
                new ChecklistTextCell { Text = string.IsNullOrWhiteSpace(r.Topic) ? "—" : r.Topic },
                new ChecklistCheckboxCell { Checked = r.PreChecked == true },
                new ChecklistCheckboxCell { Checked = r.PreChecked == false },
                new ChecklistCheckboxCell { Checked = r.ReChecked == true },
                new ChecklistCheckboxCell { Checked = r.ReChecked == false },
                new ChecklistTextCell { Text = string.IsNullOrWhiteSpace(r.Notes) ? "—" : r.Notes! },
            ]).ToList(),
        };
    }
}
