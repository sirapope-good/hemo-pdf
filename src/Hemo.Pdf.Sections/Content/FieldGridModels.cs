namespace Hemo.Pdf.Sections.Content;

public sealed class FieldGridItem
{
    public string Label { get; init; } = "";
    public string? Value { get; init; }
    public int ColumnSpan { get; init; } = 1;
}

public sealed class FieldGridModel
{
    public string? Title { get; init; }
    public int Columns { get; init; } = 2;
    public IReadOnlyList<FieldGridItem> Fields { get; init; } = [];
}

public interface IFieldGridSource
{
    FieldGridModel? Grid { get; }
}
