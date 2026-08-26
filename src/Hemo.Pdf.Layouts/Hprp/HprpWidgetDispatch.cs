using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hprp;

/// <summary>
/// Runs ordered widget ids against a handler map. One compose loop for every dedicated report;
/// reuse = share section drawers (ThaiUr, co-pay, …) as handlers, not per-report plan classes.
/// </summary>
public static class HprpWidgetDispatch
{
    private const Unit Mm = Unit.Millimetre;

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
        ArgumentNullException.ThrowIfNull(handlers);
        ComposeColumn(column, nodes, Wrap(handlers), tryGeneric);
    }

    /// <summary>
    /// Mixed plan with the current layout node (chrome / columnPlan) passed to each dense widget.
    /// </summary>
    public static void ComposeColumn(
        ColumnDescriptor column,
        IReadOnlyList<HprpLayoutNode> nodes,
        IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var node in nodes)
        {
            column.Item().Element(c => ComposeNode(c, node, handlers, tryGeneric));
        }
    }

    public static void ComposeNode(
        IContainer container,
        HprpLayoutNode node,
        IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        var type = node.Type?.Trim().ToLowerInvariant();
        if (type == "row")
        {
            ComposeRow(container, node, handlers, tryGeneric);
            return;
        }

        if (type == "column-stack")
        {
            ComposeStack(container, node, handlers, tryGeneric);
            return;
        }

        if (!string.IsNullOrWhiteSpace(node.Widget)
            && TryGetHandler(handlers, node.Widget.Trim(), out var widgetDraw))
        {
            HprpBoxComposer.Apply(container, node.Box, c => widgetDraw(c, node));
            return;
        }

        var generic = tryGeneric?.Invoke(node);
        generic?.Invoke(container);
    }

    private static void ComposeRow(
        IContainer container,
        HprpLayoutNode node,
        IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        var cells = node.Cells ?? [];
        if (cells.Count == 0)
            return;

        var tokens = cells.Select(c => string.IsNullOrWhiteSpace(c.Width) ? "*" : c.Width.Trim()).ToList();
        var parsed = HprpChrome.ParseRowCellWidths(tokens);
        var gap = node.GapMm is > 0 ? node.GapMm.Value : 0f;

        HprpBoxComposer.Apply(container, node.Box, inner =>
        {
            inner.Row(row =>
            {
                for (var i = 0; i < cells.Count; i++)
                {
                    IContainer slot;
                    if (parsed.Count == cells.Count && parsed[i].ConstantMm)
                        slot = row.ConstantItem(parsed[i].Value, Mm);
                    else if (parsed.Count == cells.Count)
                        slot = row.RelativeItem(parsed[i].Value);
                    else
                        slot = row.RelativeItem();

                    if (gap > 0)
                        slot = slot.PaddingHorizontal(gap / 2f, Mm);

                    var cell = cells[i];
                    slot.Element(c => ComposeCell(c, cell, handlers, tryGeneric));
                }
            });
        });
    }

    private static void ComposeCell(
        IContainer container,
        HprpCellNode cell,
        IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        if (cell.Nodes.Count == 0)
            return;

        if (cell.Nodes.Count == 1)
        {
            ComposeNode(container, cell.Nodes[0], handlers, tryGeneric);
            return;
        }

        container.Column(col =>
        {
            foreach (var child in cell.Nodes)
                col.Item().Element(c => ComposeNode(c, child, handlers, tryGeneric));
        });
    }

    private static void ComposeStack(
        IContainer container,
        HprpLayoutNode node,
        IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> handlers,
        Func<HprpLayoutNode, Action<IContainer>?>? tryGeneric)
    {
        HprpBoxComposer.Apply(container, node.Box, inner =>
        {
            inner.Column(col =>
            {
                foreach (var child in node.Nodes ?? [])
                    col.Item().Element(c => ComposeNode(c, child, handlers, tryGeneric));
            });
        });
    }

    private static IReadOnlyDictionary<string, Action<IContainer, HprpLayoutNode>> Wrap(
        IReadOnlyDictionary<string, Action<IContainer>> handlers)
    {
        var wrapped = new Dictionary<string, Action<IContainer, HprpLayoutNode>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in handlers)
            wrapped[pair.Key] = (container, _) => pair.Value(container);
        return wrapped;
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

    private static bool TryGetHandler<T>(
        IReadOnlyDictionary<string, T> handlers,
        string widget,
        out T draw)
        where T : class
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
