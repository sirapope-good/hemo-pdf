using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hemo.Pdf.Application;

/// <summary>
/// Merges live consent-form draft fields from request parameters onto trusted Web.Api report-data.
/// Used for inline New/Edit preview Reload so the PDF matches the final layout with current inputs.
/// </summary>
internal static class ConsentDraftOverlay
{
    public const string DraftKey = "draft";
    public const string SkeletonKey = "skeleton";
    public const string SignedByNameKey = "signedByName";
    public const string WitnessNameKey = "witnessName";
    public const string SignedDateKey = "signedDate";
    public const string PatientSignatureKey = "patientSignatureBase64";
    public const string WitnessSignatureKey = "witnessSignatureBase64";

    private static readonly string[] ThaiMonths =
    [
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม",
    ];

    private static readonly string[] EnglishMonths =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    public static JsonElement Apply(
        JsonElement data,
        IReadOnlyDictionary<string, object?> parameters)
    {
        if (data.ValueKind != JsonValueKind.Object
            || !HemosheetFetchSpec.ReadBool(parameters, DraftKey))
        {
            return data;
        }

        var node = JsonNode.Parse(data.GetRawText())?.AsObject()
            ?? new JsonObject();

        var language = ReadNodeString(node, "language")
            ?? HemosheetFetchSpec.ReadString(parameters, "lang")
            ?? "th";
        var isEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        var skeleton = HemosheetFetchSpec.ReadBool(parameters, SkeletonKey);

        // New-consent example: keep ThaiUr header identity, blank fill/sign zones as "...".
        SetProperty(node, "skeletonExample", skeleton);
        if (skeleton)
        {
            SetProperty(node, "signedByName", string.Empty);
            SetProperty(node, "isRepresentative", false);
            SetProperty(node, "witnessName", string.Empty);
            SetProperty(node, "doctorName", string.Empty);
            SetProperty(node, "nurseName", string.Empty);
            SetProperty(node, "patientSignatureBase64", (JsonNode?)null);
            SetProperty(node, "witnessSignatureBase64", (JsonNode?)null);
            SetProperty(node, "doctorSignatureBase64", (JsonNode?)null);
            SetProperty(node, "nurseSignatureBase64", (JsonNode?)null);
            SetProperty(node, "signedDate", new JsonObject
            {
                ["day"] = string.Empty,
                ["month"] = string.Empty,
                ["year"] = string.Empty,
            });
            return JsonSerializer.SerializeToElement(node);
        }

        if (parameters.ContainsKey(SignedByNameKey))
        {
            var signedBy = HemosheetFetchSpec.ReadString(parameters, SignedByNameKey)?.Trim() ?? string.Empty;
            SetProperty(node, "signedByName", signedBy);
            var patientName = ReadNodeString(node, "patientName") ?? string.Empty;
            SetProperty(node, "isRepresentative", !string.IsNullOrWhiteSpace(signedBy)
                && !string.Equals(signedBy, patientName, StringComparison.Ordinal));
        }

        if (parameters.ContainsKey(WitnessNameKey))
        {
            SetProperty(
                node,
                "witnessName",
                HemosheetFetchSpec.ReadString(parameters, WitnessNameKey)?.Trim() ?? string.Empty);
        }

        if (parameters.ContainsKey(PatientSignatureKey))
        {
            SetProperty(
                node,
                "patientSignatureBase64",
                ToNullableJsonString(HemosheetFetchSpec.ReadString(parameters, PatientSignatureKey)));
        }

        if (parameters.ContainsKey(WitnessSignatureKey))
        {
            SetProperty(
                node,
                "witnessSignatureBase64",
                ToNullableJsonString(HemosheetFetchSpec.ReadString(parameters, WitnessSignatureKey)));
        }

        if (TryParseSignedDate(parameters, out var signedLocal))
        {
            SetProperty(node, "signedDate", ToDatePartsNode(signedLocal, isEn));

            var expiryMonths = ReadNodeInt(node, "expiryMonths") ?? 0;
            if (expiryMonths > 0)
            {
                SetProperty(node, "expiryDate", ToDatePartsNode(signedLocal.AddMonths(expiryMonths), isEn));
            }
            else
            {
                SetProperty(node, "expiryDate", (JsonNode?)null);
            }
        }

        return JsonSerializer.SerializeToElement(node);
    }

    private static bool TryParseSignedDate(
        IReadOnlyDictionary<string, object?> parameters,
        out DateOnly signedLocal)
    {
        signedLocal = default;
        var raw = HemosheetFetchSpec.ReadString(parameters, SignedDateKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return DateOnly.TryParseExact(
            raw.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out signedLocal);
    }

    private static JsonObject ToDatePartsNode(DateOnly date, bool english) =>
        new()
        {
            ["day"] = date.Day.ToString(CultureInfo.InvariantCulture),
            ["month"] = english ? EnglishMonths[date.Month - 1] : ThaiMonths[date.Month - 1],
            ["year"] = english
                ? date.Year.ToString(CultureInfo.InvariantCulture)
                : (date.Year + 543).ToString(CultureInfo.InvariantCulture),
        };

    private static JsonNode? ToNullableJsonString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : JsonValue.Create(value);

    private static void SetProperty(JsonObject node, string camelName, JsonNode? value)
    {
        foreach (var key in node
            .Where(p => string.Equals(p.Key, camelName, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Key)
            .ToList())
        {
            node.Remove(key);
        }

        node[camelName] = value;
    }

    private static void SetProperty(JsonObject node, string camelName, string value) =>
        SetProperty(node, camelName, JsonValue.Create(value));

    private static void SetProperty(JsonObject node, string camelName, bool value) =>
        SetProperty(node, camelName, JsonValue.Create(value));

    private static string? ReadNodeString(JsonObject node, string propertyName)
    {
        var value = FindProperty(node, propertyName);
        if (value is null || value.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetValue<string>();
    }

    private static int? ReadNodeInt(JsonObject node, string propertyName)
    {
        var value = FindProperty(node, propertyName);
        if (value is null || value.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetValue<int>();
    }

    private static JsonNode? FindProperty(JsonObject node, string propertyName)
    {
        foreach (var prop in node)
        {
            if (string.Equals(prop.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value;
            }
        }

        return null;
    }
}
