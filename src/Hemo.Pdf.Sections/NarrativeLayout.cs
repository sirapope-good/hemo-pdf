using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections;

/// <summary>
/// Reusable layout for text-heavy clinical forms (consent and similar narrative sheets).
/// Caller supplies the header; this draws a content frame of the same width,
/// with inner padding and open leading so body copy is easy to read before signing.
/// </summary>
public static class NarrativeLayout
{
    private const Unit Mm = Unit.Millimetre;

    public const float BorderWidth = 0.4f;

    /// <summary>Gutter between the content frame and the text (ช่องไฟ).</summary>
    public const float FramePaddingMm = 7f;

    /// <summary>Open leading for long-form narrative copy.</summary>
    public const float LineHeight = 1.55f;

    /// <summary>Space between body paragraphs inside the frame.</summary>
    public const float ParagraphSpacing = 7f;

    /// <summary>
    /// Header row (natural height) then framed body. Spacing is 0 so the content
    /// border sits flush under the header and shares the same left/right edges
    /// (both fill the page content width after the page margin).
    /// Do not wrap header+body in one <c>Border</c> — the header typically already has cell borders.
    /// </summary>
    public static void Compose(
        IContainer container,
        Action<IContainer> header,
        Action<IContainer> body)
    {
        container.Column(col =>
        {
            col.Spacing(0);
            col.Item().Element(header);
            col.Item().Element(c => Frame(c, body));
        });
    }

    /// <summary>
    /// Content-only frame. When the body paginates, QuestPDF draws this border
    /// on each fragment (later pages have the frame without the header).
    /// </summary>
    public static void Frame(IContainer container, Action<IContainer> content)
    {
        container
            .Border(BorderWidth)
            .Padding(FramePaddingMm, Mm)
            .Element(content);
    }
}
