using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

/// <summary>One paragraph/line inside a designer <c>narrative</c> element (Word-lite).</summary>
public sealed class HprpNarrativeParagraph
{
    /// <summary>Paragraph body (supports soft line breaks via <c>\n</c>).</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = "";

    /// <summary>When true, indent like a sub-bullet (e.g. 2.1 …).</summary>
    [JsonPropertyName("sub")]
    public bool Sub { get; init; }

    /// <summary><c>left</c> (default) / <c>center</c> / <c>right</c>.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    /// <summary>
    /// Optional role hint for Studio / PDF styling:
    /// <c>title</c> (bold centered), <c>body</c> (default), <c>note</c> (smaller).
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; }
}
