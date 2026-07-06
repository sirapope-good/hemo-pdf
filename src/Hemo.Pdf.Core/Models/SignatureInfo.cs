using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Serialization;

namespace Hemo.Pdf.Core.Models;

public sealed class SignatureInfo
{
    public string SignerName { get; init; } = "";
    public string? SignerRole { get; init; }

    [JsonConverter(typeof(NullableByteArrayJsonConverter))]
    public byte[]? ImageBytes { get; init; }

    public DateTime? SignedAt { get; init; }
}
