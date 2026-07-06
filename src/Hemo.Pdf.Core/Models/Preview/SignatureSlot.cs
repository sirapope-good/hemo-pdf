namespace Hemo.Pdf.Core.Models.Preview;

public sealed class SignatureSlot
{
    public string Role { get; init; } = "";
    public string? Name { get; init; }
    public string? SignedAt { get; init; }
    public string? ImageUrl { get; init; }
}
