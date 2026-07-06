namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportFooterBlock
{
    public string Type { get; init; } = "page-number";
    public IReadOnlyList<string> Lines { get; init; } = [];
    public PageNumberInfo? PageNumber { get; init; }
    public IReadOnlyList<SignatureSlot> Signatures { get; init; } = [];
}

public sealed class PageNumberInfo
{
    public int Current { get; init; }
    public int Total { get; init; }
}
