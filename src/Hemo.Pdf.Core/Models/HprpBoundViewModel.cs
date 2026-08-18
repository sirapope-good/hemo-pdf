using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Models;

/// <summary>Bound layout produced by <c>.hprp</c> for default/scaffold clinical reports.</summary>
public sealed class HprpBoundViewModel
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public IReadOnlyList<ReportBlock> Blocks { get; init; } = [];
}
