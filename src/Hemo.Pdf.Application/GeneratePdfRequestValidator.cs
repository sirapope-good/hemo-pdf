using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

public static class GeneratePdfRequestValidator
{
    public static void Validate(
        GeneratePdfRequest request,
        ITenantContextAccessor tenantContext,
        bool allowMissingData = false)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trustedTenant = tenantContext.TenantCode;
        if (string.IsNullOrWhiteSpace(trustedTenant))
        {
            throw new PdfGenerationBadRequestException("Tenant code is required.");
        }

        if (!TenantCodeNormalizer.EqualsNormalized(request.TenantCode, trustedTenant))
        {
            throw new PdfGenerationForbiddenException(
                "Request tenantCode does not match the authenticated tenant.");
        }

        if (string.IsNullOrWhiteSpace(request.EntityId))
        {
            throw new PdfGenerationBadRequestException("entityId is required.");
        }

        var hasData = request.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        if (!hasData)
        {
            if (!allowMissingData)
            {
                throw new PdfGenerationBadRequestException("data is required.");
            }

            return;
        }

        // Template payloads use Guid.Empty (or other placeholder ids) while FE correlates with
        // entityId like "template-{unitId}" — skip id equality for template requests.
        if (HemosheetFetchSpec.IsTemplateRequest(request))
        {
            return;
        }

        if (TryGetDataId(request.Data, out var dataId)
            && !string.Equals(dataId, request.EntityId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfGenerationBadRequestException("entityId must match data.id.");
        }
    }

    private static bool TryGetDataId(JsonElement data, out string id)
    {
        id = string.Empty;
        if (data.ValueKind != JsonValueKind.Object)
            return false;

        if (!data.TryGetProperty("id", out var idElement))
            return false;

        if (idElement.ValueKind == JsonValueKind.String)
        {
            id = idElement.GetString()?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(id);
        }

        if (idElement.ValueKind is JsonValueKind.Number)
        {
            id = idElement.ToString();
            return !string.IsNullOrEmpty(id);
        }

        return false;
    }
}
