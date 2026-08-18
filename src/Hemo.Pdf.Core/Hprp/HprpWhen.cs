using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpWhen
{
    public static bool Matches(JsonElement when, Func<string, bool> predicate)
    {
        if (when.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return true;

        if (when.ValueKind == JsonValueKind.String)
        {
            var text = when.GetString();
            return string.IsNullOrWhiteSpace(text) || predicate(text!);
        }

        if (when.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in when.EnumerateArray())
            {
                if (!Matches(item, predicate))
                    return false;
            }

            return true;
        }

        return true;
    }

    public static bool MatchesDto(JsonElement when, JsonElement? data)
    {
        return Matches(when, token => EvaluateDtoToken(token, data));
    }

    private static bool EvaluateDtoToken(string token, JsonElement? data)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
            return true;

        var comparison = SplitComparison(trimmed);
        var selected = HprpJsonPath.Select(data, comparison.Path);
        if (comparison.Operator is null)
            return HprpJsonPath.IsTruthy(selected);

        var left = HprpJsonPath.AsString(selected);
        return comparison.Operator switch
        {
            ">" => TryDecimal(left, out var l) && TryDecimal(comparison.Right, out var r) && l > r,
            ">=" => TryDecimal(left, out var l) && TryDecimal(comparison.Right, out var r) && l >= r,
            "<" => TryDecimal(left, out var l) && TryDecimal(comparison.Right, out var r) && l < r,
            "<=" => TryDecimal(left, out var l) && TryDecimal(comparison.Right, out var r) && l <= r,
            "!=" => !string.Equals(left, comparison.Right, StringComparison.OrdinalIgnoreCase),
            "==" => string.Equals(left, comparison.Right, StringComparison.OrdinalIgnoreCase),
            _ => HprpJsonPath.IsTruthy(selected),
        };
    }

    private static (string Path, string? Operator, string? Right) SplitComparison(string token)
    {
        foreach (var op in new[] { ">=", "<=", "!=", "==", ">", "<" })
        {
            var index = token.IndexOf(op, StringComparison.Ordinal);
            if (index <= 0)
                continue;

            return (token[..index].Trim(), op, token[(index + op.Length)..].Trim());
        }

        return (token, null, null);
    }

    private static bool TryDecimal(string? text, out decimal value) =>
        decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
}
