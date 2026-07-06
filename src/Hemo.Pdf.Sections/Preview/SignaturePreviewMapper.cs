using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview;

public static class SignaturePreviewMapper
{
    public static SignatureReportBlock? Map(PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
        if (signatures.Count == 0)
        {
            return null;
        }

        return new SignatureReportBlock
        {
            Slots = MapSlots(signatures),
        };
    }

    public static IReadOnlyList<SignatureSlot> MapSlots(IReadOnlyList<SignatureInfo> signatures) =>
        signatures
            .Select(signature => new SignatureSlot
            {
                Role = string.IsNullOrWhiteSpace(signature.SignerRole) ? "ลายเซ็น" : signature.SignerRole!,
                Name = signature.SignerName,
                SignedAt = signature.SignedAt?.ToString("o"),
                ImageUrl = ToDataUrl(signature.ImageBytes),
            })
            .ToList();

    private static string? ToDataUrl(byte[]? imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            return null;
        }

        return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
    }
}
