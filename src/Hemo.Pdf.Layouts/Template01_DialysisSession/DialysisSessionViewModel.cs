using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Layouts.Template01_DialysisSession;

public sealed class DialysisSessionViewModel : IPatientInfoSource, IDataGridSource, IKeyValueRowsSource
{
    public PatientInfoModel PatientInfo { get; init; } = new();
    public DataGridModel? Grid { get; init; }
    public IReadOnlyList<KeyValuePair<string, string?>> SessionRows { get; init; } = [];
    public IReadOnlyList<KeyValuePair<string, string?>> Rows => SessionRows;
    public string? SectionTitle => "รายละเอียดการฟอกไต";
}
