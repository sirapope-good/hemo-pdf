namespace Hemo.Pdf.Sections.Content;

public sealed class DataGridModel
{
    public string? Title { get; init; }
    public IReadOnlyList<string> ColumnHeaders { get; init; } = [];
    public IReadOnlyList<float> ColumnWeights { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; } = [];
    public Hemo.Pdf.Core.Hprp.HprpChrome? Chrome { get; init; }
}

public interface IDataGridSource
{
    DataGridModel? Grid { get; }
}
