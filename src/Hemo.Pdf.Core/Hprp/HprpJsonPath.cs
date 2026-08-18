using System.Globalization;
using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Tiny JSONPath subset: <c>$</c>, <c>$.a.b</c>, <c>$.items[0].name</c>, <c>$.items.length</c>.
/// </summary>
public static class HprpJsonPath
{
    public static JsonElement? Select(JsonElement? root, string? path)
    {
        if (root is null || string.IsNullOrWhiteSpace(path))
            return null;

        var trimmed = path.Trim();
        if (trimmed == "$")
            return root;

        if (!trimmed.StartsWith("$.", StringComparison.Ordinal))
            return null;

        var current = root.Value;
        var tokens = trimmed[2..].Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (!TryStep(ref current, token))
                return null;
        }

        return current;
    }

    public static string? AsString(JsonElement? element)
    {
        if (element is null)
            return null;

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => value.GetRawText(),
        };
    }

    public static bool IsTruthy(JsonElement? element)
    {
        if (element is null)
            return false;

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => false,
            JsonValueKind.False => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Number => value.TryGetDecimal(out var n) && n != 0,
            JsonValueKind.Array => value.GetArrayLength() > 0,
            JsonValueKind.Object => true,
            JsonValueKind.True => true,
            _ => true,
        };
    }

    private static bool TryStep(ref JsonElement current, string token)
    {
        var name = token;
        int? index = null;
        var bracket = token.IndexOf('[', StringComparison.Ordinal);
        if (bracket >= 0)
        {
            var close = token.IndexOf(']', bracket);
            if (close < 0)
                return false;

            name = token[..bracket];
            if (!int.TryParse(token[(bracket + 1)..close], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                return false;
            index = parsed;
        }

        if (string.Equals(name, "length", StringComparison.OrdinalIgnoreCase)
            && current.ValueKind == JsonValueKind.Array
            && index is null)
        {
            current = JsonSerializer.SerializeToElement(current.GetArrayLength());
            return true;
        }

        if (current.ValueKind != JsonValueKind.Object)
            return false;

        if (!current.TryGetProperty(name, out var next)
            && !TryGetPropertyIgnoreCase(current, name, out next))
        {
            return false;
        }

        current = next;
        if (index is null)
            return true;

        if (current.ValueKind != JsonValueKind.Array)
            return false;

        if (index.Value < 0 || index.Value >= current.GetArrayLength())
            return false;

        current = current.EnumerateArray().ElementAt(index.Value);
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
