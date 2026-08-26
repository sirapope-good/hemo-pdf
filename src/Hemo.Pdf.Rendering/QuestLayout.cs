using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Rendering;

public sealed class QuestLayout
{
    public Action<IContainer>? Header { get; init; }
    public Action<IContainer>? Content { get; init; }
    public Action<IContainer>? Footer { get; init; }

    public float MarginMillimeters { get; init; } = 10f;

    public float? MarginTop { get; init; } = 3f;
    public float? MarginBottom { get; init; } = 3f;
    public float? MarginLeft { get; init; }
    public float? MarginRight { get; init; }

    /// <summary>
    /// Tenant section/column header fill applied for the QuestPDF render thread.
    /// Null keeps each layout's built-in fallback color.
    /// </summary>
    public string? SectionHeaderBackground { get; set; }

    /// <summary>When true, renders A4 landscape (width &gt; height).</summary>
    public bool Landscape { get; init; }
}
