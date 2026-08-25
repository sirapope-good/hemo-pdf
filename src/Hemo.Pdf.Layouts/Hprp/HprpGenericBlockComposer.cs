using System.Text.Json;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hprp;

/// <summary>
/// Turns extra HPRP <c>type</c> nodes into QuestPDF drawers so dedicated reports
/// can mix dense widgets with form blocks without a C# rebuild.
/// </summary>
public static class HprpGenericBlockComposer
{
    public static Action<IContainer>? TryCreateDrawer(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext context)
    {
        if (!HprpLayoutPlan.IsGenericBlock(node))
            return null;

        var block = HprpBinder.BindGeneric(node, data, labels, context);
        if (block is null)
            return null;

        return container => ReportBlockPdfComposer.Compose(container, block, context);
    }
}
