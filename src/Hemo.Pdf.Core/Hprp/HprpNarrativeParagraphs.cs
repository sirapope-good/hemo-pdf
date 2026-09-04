using System.Text.Json;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>Resolves pack + bound paragraphs for designer <c>narrative</c>.</summary>
public static class HprpNarrativeParagraphs
{
    public static IReadOnlyList<HprpNarrativeParagraph> Resolve(
        HprpDesignerElement element,
        JsonElement? data)
    {
        var pack = element.Paragraphs ?? Array.Empty<HprpNarrativeParagraph>();

        if (!string.IsNullOrWhiteSpace(element.BindParagraphs)
            && data is JsonElement root
            && root.ValueKind == JsonValueKind.Object)
        {
            var bound = TryReadBound(root, element.BindParagraphs.Trim());
            if (bound is { Count: > 0 })
            {
                // Keep pack title lines; bound body replaces the rest.
                var titles = pack
                    .Where(p => string.Equals(p.Role?.Trim(), "title", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (titles.Count == 0)
                    return bound;

                var merged = new List<HprpNarrativeParagraph>(titles.Count + bound.Count);
                merged.AddRange(titles);
                merged.AddRange(bound);
                return merged;
            }
        }

        return pack;
    }

    private static IReadOnlyList<HprpNarrativeParagraph>? TryReadBound(JsonElement root, string path)
    {
        var node = Traverse(root, path);
        if (node is null || node.Value.ValueKind != JsonValueKind.Array)
            return null;

        var list = new List<HprpNarrativeParagraph>();
        foreach (var item in node.Value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString() ?? "";
                if (s.Length > 0)
                    list.Add(new HprpNarrativeParagraph { Text = s });
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var text = ReadString(item, "text") ?? "";
            if (string.IsNullOrWhiteSpace(text))
                continue;

            list.Add(new HprpNarrativeParagraph
            {
                Text = text,
                Sub = ReadBool(item, "sub"),
                Align = ReadString(item, "align"),
                Role = ReadString(item, "role"),
            });
        }

        return list;
    }

    private static JsonElement? Traverse(JsonElement root, string path)
    {
        var p = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path.TrimStart('$', '.');
        if (string.IsNullOrWhiteSpace(p))
            return root;

        var cur = root;
        foreach (var seg in p.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(seg, out var next))
                return null;
            cur = next;
        }

        return cur;
    }

    private static string? ReadString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
    }

    private static bool ReadBool(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return false;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(el.GetString(), out var b) && b,
            _ => false,
        };
    }
}
