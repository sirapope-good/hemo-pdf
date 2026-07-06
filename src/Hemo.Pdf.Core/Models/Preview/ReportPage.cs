namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportPage
{
    public IReadOnlyList<ReportBlock> Blocks { get; init; } = [];
}
