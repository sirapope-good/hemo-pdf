using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

/// <summary>Field tree node for Studio data mapper (from adapter schema JSON).</summary>
public sealed class HprpAdapterFieldNode
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("label")]
    public string Label { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "string";

    [JsonPropertyName("children")]
    public IReadOnlyList<HprpAdapterFieldNode> Children { get; init; } = [];
}

public sealed class HprpAdapterSchema
{
    [JsonPropertyName("dataAdapter")]
    public string DataAdapter { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("fields")]
    public IReadOnlyList<HprpAdapterFieldNode> Fields { get; init; } = [];
}

/// <summary>Resolves binding paths against JSON report-data.</summary>
public static class HprpJsonPath
{
    public static string? ReadAt(
        JsonElement root,
        string path,
        int groupIndex = -1,
        int slotIndex = -1,
        int freedomRowIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(path) || root.ValueKind != JsonValueKind.Object)
            return null;

        var segments = ParseSegments(path);
        if (segments.Count == 0)
            return null;

        return Walk(root, segments, 0, groupIndex, slotIndex, freedomRowIndex);
    }

    public static bool ReadBoolAt(
        JsonElement root,
        string path,
        int groupIndex,
        int slotIndex)
    {
        var text = ReadAt(root, path, groupIndex, slotIndex);
        return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Walk(
        JsonElement current,
        IReadOnlyList<Segment> segments,
        int index,
        int groupIndex,
        int slotIndex,
        int freedomRowIndex)
    {
        if (index >= segments.Count)
            return FormatValue(current);

        var seg = segments[index];
        if (seg.IsArray)
        {
            if (current.ValueKind != JsonValueKind.Array && current.ValueKind != JsonValueKind.Object)
                return null;

            JsonElement arrayEl;
            if (current.ValueKind == JsonValueKind.Array)
            {
                arrayEl = current;
            }
            else if (!current.TryGetProperty(seg.Name, out arrayEl))
            {
                return null;
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
                return null;

            var arrIndex = seg.Wildcard
                ? seg.Name.Contains("entries", StringComparison.OrdinalIgnoreCase)
                    ? slotIndex
                    : groupIndex
                : seg.ArrayIndex;

            if (arrIndex < 0)
                return null;

            var i = 0;
            foreach (var item in arrayEl.EnumerateArray())
            {
                if (i == arrIndex)
                    return Walk(item, segments, index + 1, groupIndex, slotIndex, freedomRowIndex);
                i++;
            }

            return null;
        }

        if (current.ValueKind != JsonValueKind.Object)
            return null;

        if (!current.TryGetProperty(seg.Name, out var next))
            return null;

        return Walk(next, segments, index + 1, groupIndex, slotIndex, freedomRowIndex);
    }

    private static string? FormatValue(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => el.GetRawText(),
        };

    private sealed record Segment(string Name, bool IsArray, bool Wildcard, int ArrayIndex);

    private static List<Segment> ParseSegments(string path)
    {
        var list = new List<Segment>();
        foreach (var raw in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bracket = raw.IndexOf('[', StringComparison.Ordinal);
            if (bracket < 0)
            {
                list.Add(new Segment(raw, false, false, -1));
                continue;
            }

            var name = raw[..bracket];
            var inside = raw[(bracket + 1)..].TrimEnd(']');
            if (inside.Length == 0)
            {
                list.Add(new Segment(name, true, true, -1));
            }
            else if (int.TryParse(inside, out var idx))
            {
                list.Add(new Segment(name, true, false, idx));
            }
            else
            {
                list.Add(new Segment(name, true, true, -1));
            }
        }

        return list;
    }
}
