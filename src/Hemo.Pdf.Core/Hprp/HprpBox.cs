using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>Optional four-side inset in millimetres.</summary>
public sealed class HprpSides
{
    [JsonPropertyName("top")]
    public float? Top { get; init; }

    [JsonPropertyName("right")]
    public float? Right { get; init; }

    [JsonPropertyName("bottom")]
    public float? Bottom { get; init; }

    [JsonPropertyName("left")]
    public float? Left { get; init; }

    public bool HasAny => Top is not null || Right is not null || Bottom is not null || Left is not null;
}

/// <summary>
/// Per-node margin/padding. <c>marginMm</c> / <c>paddingMm</c> accept a number,
/// CSS-style array (1/2/4 values), or <c>{top,right,bottom,left}</c>.
/// </summary>
public sealed class HprpNodeBox
{
    [JsonPropertyName("marginMm")]
    public JsonElement MarginMm { get; init; }

    [JsonPropertyName("paddingMm")]
    public JsonElement PaddingMm { get; init; }

    public bool IsEmpty =>
        MarginMm.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        && PaddingMm.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null;
}

public static class HprpBox
{
    public const float MaxMm = 80f;

    public static HprpSides? TryParseSides(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (!TryReadMm(element, out var all))
                return null;
            return Uniform(all);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var values = new List<float>();
            foreach (var item in element.EnumerateArray())
            {
                if (!TryReadMm(item, out var mm))
                    return null;
                values.Add(mm);
            }

            return values.Count switch
            {
                1 => Uniform(values[0]),
                2 => new HprpSides
                {
                    Top = values[0],
                    Bottom = values[0],
                    Right = values[1],
                    Left = values[1],
                },
                4 => new HprpSides
                {
                    Top = values[0],
                    Right = values[1],
                    Bottom = values[2],
                    Left = values[3],
                },
                _ => null,
            };
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        return new HprpSides
        {
            Top = ReadNamed(element, "top"),
            Right = ReadNamed(element, "right"),
            Bottom = ReadNamed(element, "bottom"),
            Left = ReadNamed(element, "left"),
        };
    }

    public static void Validate(HprpNodeBox? box, string path, List<string> errors)
    {
        if (box is null || box.IsEmpty)
            return;

        ValidateElement(box.MarginMm, path + ".marginMm", errors);
        ValidateElement(box.PaddingMm, path + ".paddingMm", errors);
    }

    public static void ValidateSides(HprpSides? sides, string path, List<string> errors)
    {
        if (sides is null)
            return;

        ValidateMm(sides.Top, path + ".top", errors);
        ValidateMm(sides.Right, path + ".right", errors);
        ValidateMm(sides.Bottom, path + ".bottom", errors);
        ValidateMm(sides.Left, path + ".left", errors);
    }

    private static void ValidateElement(JsonElement element, string path, List<string> errors)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return;

        if (TryParseSides(element) is null)
            errors.Add($"{path} must be a number, [t,r,b,l] / [v,h], or {{top,right,bottom,left}} in mm.");
        else
            ValidateSides(TryParseSides(element), path, errors);
    }

    private static void ValidateMm(float? value, string path, List<string> errors)
    {
        if (value is < 0 or > MaxMm)
            errors.Add($"{path} must be between 0 and {MaxMm} mm.");
    }

    private static HprpSides Uniform(float mm) => new()
    {
        Top = mm,
        Right = mm,
        Bottom = mm,
        Left = mm,
    };

    private static float? ReadNamed(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var prop))
            return null;
        return TryReadMm(prop, out var mm) ? mm : null;
    }

    private static bool TryReadMm(JsonElement element, out float mm)
    {
        mm = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out var n)
            && n is >= 0 and <= MaxMm && float.IsFinite(n))
        {
            mm = n;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString()?.Trim() ?? "";
            if (raw.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
                raw = raw[..^2].Trim();

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out n)
                && n is >= 0 and <= MaxMm && float.IsFinite(n))
            {
                mm = n;
                return true;
            }
        }

        return false;
    }
}
