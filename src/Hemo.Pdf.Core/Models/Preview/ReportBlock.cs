using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Models.Preview;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PatientInfoReportBlock), "patient-info")]
[JsonDerivedType(typeof(KeyValueTableReportBlock), "key-value-table")]
[JsonDerivedType(typeof(DataGridReportBlock), "data-grid")]
[JsonDerivedType(typeof(ChecklistTableReportBlock), "checklist-table")]
[JsonDerivedType(typeof(VascularAccessReportBlock), "vascular-access")]
[JsonDerivedType(typeof(FieldGridReportBlock), "field-grid")]
[JsonDerivedType(typeof(SignatureReportBlock), "signature")]
[JsonDerivedType(typeof(TextReportBlock), "text")]
public abstract class ReportBlock;

public sealed class PatientInfoReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public IReadOnlyList<IReadOnlyList<LabelValue>> Columns { get; init; } = [];
}

public sealed class KeyValueTableReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public IReadOnlyList<LabelValue> Rows { get; init; } = [];
}

public sealed class DataGridReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<float> ColumnWeights { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
}

public sealed class ChecklistTableReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<ChecklistCellValue>> Rows { get; init; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChecklistTextCell), "text")]
[JsonDerivedType(typeof(ChecklistCheckboxCell), "checkbox")]
public abstract class ChecklistCellValue;

public sealed class ChecklistTextCell : ChecklistCellValue
{
    public string Text { get; init; } = "";
}

public sealed class ChecklistCheckboxCell : ChecklistCellValue
{
    public bool Checked { get; init; }
    public string? Label { get; init; }
}

public sealed class SignatureReportBlock : ReportBlock
{
    public IReadOnlyList<SignatureSlot> Slots { get; init; } = [];
}

public sealed class TextReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public string Content { get; init; } = "";
    public string Style { get; init; } = "body";
}

public sealed class FieldGridReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public int Columns { get; init; } = 2;
    public IReadOnlyList<FieldGridField> Fields { get; init; } = [];
}

public sealed class FieldGridField
{
    public string Label { get; init; } = "";
    public string? Value { get; init; }
    public int ColumnSpan { get; init; } = 1;
}

public sealed class VascularAccessReportBlock : ReportBlock
{
    public string? Title { get; init; }
    public string Variant { get; init; } = "av-fistula";
    public IReadOnlyList<LabelValue> Rows { get; init; } = [];
}
