using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Layouts.Hemosheet;

internal static class HemosheetSectionAdapters
{
    internal sealed class PatientInfoAdapter(HemosheetReportViewModel vm) : IPatientInfoSource
    {
        public PatientInfoModel PatientInfo { get; } = new()
        {
            Name = vm.Patient.Name,
            HospitalNumber = vm.Patient.Hn,
            IdentityNumber = vm.Patient.IdentityNumber,
            DateOfBirth = vm.Patient.BirthDate?.ToString("yyyy-MM-dd"),
            Gender = vm.Patient.Sex,
            Unit = vm.Unit.FullName,
        };
    }

    internal sealed class KeyValueRowsAdapter(string? title, IReadOnlyList<LabelValue> rows) : IKeyValueRowsSource
    {
        public string? SectionTitle => title;

        public IReadOnlyList<KeyValuePair<string, string?>> Rows =>
            rows.Select(r => new KeyValuePair<string, string?>(r.Label, r.Value)).ToList();
    }

    internal sealed class ChecklistAdapter(string title, IList<HemosheetAssessmentItemViewModel> items) : IChecklistSource
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

    internal sealed class DataGridAdapter(DataGridReportBlock block) : IDataGridSource
    {
        public DataGridModel? Grid { get; } = new()
        {
            Title = block.Title,
            ColumnHeaders = block.Columns.ToList(),
            ColumnWeights = block.ColumnWeights.ToList(),
            Rows = block.Rows.Select(row => row.Select(v => (string?)v).ToList()).ToList(),
        };
    }

    internal sealed class FieldGridAdapter(FieldGridReportBlock block) : IFieldGridSource
    {
        public FieldGridModel? Grid { get; } = new()
        {
            Title = block.Title,
            Columns = block.Columns,
            Fields = block.Fields.Select(f => new FieldGridItem
            {
                Label = f.Label,
                Value = f.Value,
                ColumnSpan = f.ColumnSpan,
            }).ToList(),
        };
    }
}
