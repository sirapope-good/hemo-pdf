namespace Hemo.Pdf.Sections.Content;

public sealed class ChecklistItem
{
    public string Label { get; init; } = "";
    public bool IsChecked { get; init; }
    public string? Notes { get; init; }
}

public sealed class ChecklistTableModel
{
    public string? Title { get; init; }
    public IReadOnlyList<ChecklistItem> Items { get; init; } = [];
}

public interface IChecklistSource
{
    ChecklistTableModel? Checklist { get; }
}
