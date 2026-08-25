using Hemo.Pdf.Core.Hprp;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hprp;

/// <summary>
/// Runs ordered widget ids against a handler map. One compose loop for every dedicated report;
/// reuse = share section drawers (ThaiUr, co-pay, …) as handlers, not per-report plan classes.
/// </summary>
public static class HprpWidgetDispatch
{
    /// <summary>
    /// Emit each known widget as a column item. Unknown ids are skipped (forward-compatible packages).
    /// </summary>
    public static void ComposeColumn(
        ColumnDescriptor column,
        IReadOnlyList<string> widgets,
        IReadOnlyDictionary<string, Action<IContainer>> handlers)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(widgets);
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var widget in widgets)
        {
            if (!TryGetHandler(handlers, widget, out var draw))
                continue;

            column.Item().Element(draw);
        }
    }

    /// <summary>
    /// Mixed plan: allowed dense widgets via <paramref name="handlers"/>, extra form
    /// blocks via <paramref name="tryGeneric"/>. Null generic drawers are skipped.
    /// </summary>
    public static void ComposeColumn(
        ColumnDescriptor column,
        IReadOnlyList<HprpLayoutNode> nodes,
        IReadOnlyDictionary<string, Action<IContainer>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Widget)
                && TryGetHandler(handlers, node.Widget.Trim(), out var draw))
            {
                column.Item().Element(draw);
                continue;
            }

            var generic = tryGeneric?.Invoke(node);
            if (generic is not null)
                column.Item().Element(generic);
        }
    }

    /// <summary>Invoke handlers in order without wrapping in a QuestPDF column.</summary>
    public static void ForEach(
        IReadOnlyList<string> widgets,
        IReadOnlyDictionary<string, Action<IContainer>> handlers,
        Action<Action<IContainer>> emit)
    {
        ArgumentNullException.ThrowIfNull(widgets);
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(emit);

        foreach (var widget in widgets)
        {
            if (!TryGetHandler(handlers, widget, out var draw))
                continue;

            emit(draw);
        }
    }

    private static bool TryGetHandler(
        IReadOnlyDictionary<string, Action<IContainer>> handlers,
        string widget,
        out Action<IContainer> draw)
    {
        if (handlers.TryGetValue(widget, out draw!))
            return true;

        foreach (var pair in handlers)
        {
            if (string.Equals(pair.Key, widget, StringComparison.OrdinalIgnoreCase))
            {
                draw = pair.Value;
                return true;
            }
        }

        draw = null!;
        return false;
    }
}
