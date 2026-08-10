using System.Globalization;
using System.Text.Json;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

/// <summary>
/// Typed read of hemosheet <see cref="GeneratePdfRequest.Parameters"/> (template vs record).
/// </summary>
public sealed record HemosheetFetchSpec(
    bool IsTemplate,
    int? UnitId,
    string TemplateMode,
    bool TcvUsePercent)
{
    public const string TemplateKey = "template";
    public const string UnitIdKey = "unitId";
    public const string TemplateModeKey = "templateMode";
    public const string TcvUsePercentKey = "tcvUsePercent";

    public static HemosheetFetchSpec FromRequest(GeneratePdfRequest request)
    {
        var parameters = request.Parameters ?? new Dictionary<string, object?>();
        var isTemplate = ReadBool(parameters, TemplateKey);
        var tcvUsePercent = ReadBool(parameters, TcvUsePercentKey);
        var templateMode = ReadString(parameters, TemplateModeKey) ?? "hd";
        int? unitId = null;

        if (isTemplate)
        {
            unitId = ReadInt(parameters, UnitIdKey);
            if (unitId is null)
            {
                throw new PdfGenerationBadRequestException("parameters.unitId is required for template preview.");
            }
        }

        return new HemosheetFetchSpec(isTemplate, unitId, templateMode, tcvUsePercent);
    }

    public static bool IsTemplateRequest(GeneratePdfRequest request) =>
        ReadBool(request.Parameters ?? new Dictionary<string, object?>(), TemplateKey);

    public static bool ReadBool(IReadOnlyDictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
            return false;

        return value switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false,
        };
    }

    public static int? ReadInt(IReadOnlyDictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            JsonElement el when el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n) => n,
            JsonElement el when el.ValueKind == JsonValueKind.String
                && int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromJsonString) => fromJsonString,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    public static string? ReadString(IReadOnlyDictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
            _ => value.ToString(),
        };
    }
}
