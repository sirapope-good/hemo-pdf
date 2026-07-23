using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

/// <summary>
/// Resolves hemosheet signature state from trusted report-data payload fields.
/// </summary>
public sealed class HemoproSignatureStore : ISignatureStore
{
    public Task<ReportSignatureContext> GetAsync(
        string reportTemplateId,
        string entityId,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ReportSignatureContext
        {
            IsFullySigned = false,
            Signatures = [],
        });
    }

    public static ReportSignatureContext? TryResolveFromData(string reportTemplateId, JsonElement data)
    {
        if (!string.Equals(reportTemplateId, ReportTemplates.Hemosheet, StringComparison.OrdinalIgnoreCase)
            || data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var hasDoctorSignature = data.TryGetProperty("doctorSignatureBase64", out var doctorSig)
            && doctorSig.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(doctorSig.GetString());

        var signatureCount = 0;
        JsonElement names = default;
        if (data.TryGetProperty("signatureNames", out names) && names.ValueKind == JsonValueKind.Object)
        {
            signatureCount = names.EnumerateObject().Count();
        }

        if (!hasDoctorSignature && signatureCount == 0)
        {
            return null;
        }

        var signatures = new List<SignatureInfo>();
        if (hasDoctorSignature && data.TryGetProperty("doctorName", out var doctorName)
            && doctorName.ValueKind == JsonValueKind.String)
        {
            signatures.Add(new SignatureInfo
            {
                SignerName = doctorName.GetString(),
                SignerRole = "Doctor",
                SignedAt = DateTime.UtcNow,
            });
        }

        if (names.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in names.EnumerateObject())
            {
                signatures.Add(new SignatureInfo
                {
                    SignerRole = entry.Name,
                    SignerName = entry.Value.GetString(),
                    SignedAt = DateTime.UtcNow,
                });
            }
        }

        return new ReportSignatureContext
        {
            IsFullySigned = hasDoctorSignature || signatureCount > 0,
            Signatures = signatures,
        };
    }
}
