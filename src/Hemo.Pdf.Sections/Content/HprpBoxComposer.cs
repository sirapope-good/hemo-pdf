using Hemo.Pdf.Core.Hprp;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public static class HprpBoxComposer
{
    private const Unit Mm = Unit.Millimetre;

    public static void Apply(IContainer container, HprpNodeBox? box, Action<IContainer> content)
    {
        if (box is null || box.IsEmpty)
        {
            content(container);
            return;
        }

        var padded = ApplySides(container, HprpBox.TryParseSides(box.MarginMm));
        padded = ApplySides(padded, HprpBox.TryParseSides(box.PaddingMm));
        content(padded);
    }

    private static IContainer ApplySides(IContainer container, HprpSides? sides)
    {
        if (sides is null || !sides.HasAny)
            return container;

        var next = container;
        if (sides.Top is > 0)
            next = next.PaddingTop(sides.Top.Value, Mm);
        if (sides.Right is > 0)
            next = next.PaddingRight(sides.Right.Value, Mm);
        if (sides.Bottom is > 0)
            next = next.PaddingBottom(sides.Bottom.Value, Mm);
        if (sides.Left is > 0)
            next = next.PaddingLeft(sides.Left.Value, Mm);
        return next;
    }
}
