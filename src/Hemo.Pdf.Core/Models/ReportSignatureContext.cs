namespace Hemo.Pdf.Core.Models;

public sealed class ReportSignatureContext
{
    public bool IsFullySigned { get; init; }
    public IReadOnlyList<SignatureInfo> Signatures { get; init; } = [];
}
