using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Layouts.Hprp;

/// <summary>
/// Shared resolve of widget order from an <c>.hprp</c> package.
/// Dedicated composers supply <paramref name="defaults"/> + <paramref name="allowed"/>;
/// pixel drawing stays in section handlers registered per report.
/// </summary>
public static class HprpLayoutPlan
{
    public static HprpPackage? TryGetPackage(IHprpTemplateStore? store, PdfReportContext context)
    {
        if (context.LayoutPackage is not null)
            return context.LayoutPackage;

        var templateId = ClinicalReportCatalog.ResolveEngineTemplateId(context.ReportTemplateId);
        return store?.TryGetCached(context.TenantCode, templateId);
    }

    /// <summary>
    /// Ordered widgets from <c>layout.header</c> then <c>layout.body</c>.
    /// Empty / unknown packages return <paramref name="defaults"/>.
    /// Generic <c>type</c> blocks are ignored here — use <see cref="ResolveNodes"/>.
    /// </summary>
    public static IReadOnlyList<string> ResolveWidgetOrder(
        HprpPackage? package,
        IReadOnlyList<string> defaults,
        IReadOnlySet<string>? allowed = null)
    {
        var widgets = new List<string>();
        foreach (var node in ResolveNodes(package, defaults, allowed, includeHeader: true))
        {
            if (!string.IsNullOrWhiteSpace(node.Widget))
                widgets.Add(node.Widget.Trim());
        }

        return widgets.Count > 0 ? widgets : defaults;
    }

    /// <summary>
    /// Header (optional) then body: allowed dense widgets <b>and</b> generic form blocks
    /// (<c>text</c>, <c>key-value-table</c>, …). Empty / unknown packages return
    /// <paramref name="defaults"/> as widget-only nodes.
    /// </summary>
    public static IReadOnlyList<HprpLayoutNode> ResolveNodes(
        HprpPackage? package,
        IReadOnlyList<string> defaults,
        IReadOnlySet<string>? allowed = null,
        bool includeHeader = true)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        if (defaults.Count == 0)
            throw new ArgumentException("defaults must not be empty.", nameof(defaults));

        var allow = allowed ?? ToAllowSet(defaults);
        if (package is null)
            return ToWidgetNodes(defaults);

        var nodes = new List<HprpLayoutNode>();
        if (includeHeader && package.Layout.Header is { } header && ShouldKeep(header, allow))
            nodes.Add(header);

        foreach (var node in package.Layout.Body)
        {
            if (ShouldKeep(node, allow))
                nodes.Add(node);
        }

        return nodes.Count > 0 ? nodes : ToWidgetNodes(defaults);
    }

    /// <summary>Single header widget id, or <paramref name="fallback"/> when missing.</summary>
    public static string ResolveHeaderWidget(
        HprpPackage? package,
        string fallback,
        IReadOnlySet<string>? allowed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        if (package?.Layout.Header?.Widget is not { } raw || string.IsNullOrWhiteSpace(raw))
            return fallback;

        var id = raw.Trim();
        var allow = allowed ?? ToAllowSet([fallback]);
        if (!HprpWidgetIds.All.Contains(id) || !allow.Contains(id))
            return fallback;

        return id;
    }

    /// <summary>Body widgets only (no header). Empty package → <paramref name="defaults"/>.</summary>
    public static IReadOnlyList<string> ResolveBodyWidgets(
        HprpPackage? package,
        IReadOnlyList<string> defaults,
        IReadOnlySet<string>? allowed = null)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        if (defaults.Count == 0)
            throw new ArgumentException("defaults must not be empty.", nameof(defaults));

        var widgets = new List<string>();
        foreach (var node in ResolveNodes(package, defaults, allowed, includeHeader: false))
        {
            if (!string.IsNullOrWhiteSpace(node.Widget))
                widgets.Add(node.Widget.Trim());
        }

        return widgets.Count > 0 ? widgets : defaults;
    }

    public static bool IsGenericBlock(HprpLayoutNode node) =>
        HprpWidgetIds.IsBlockType(node.Type);

    private static HashSet<string> ToAllowSet(IReadOnlyList<string> defaults) =>
        new(defaults, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<HprpLayoutNode> ToWidgetNodes(IReadOnlyList<string> widgets) =>
        widgets.Select(w => new HprpLayoutNode { Widget = w }).ToList();

    /// <summary>
    /// Widget nodes must be on the report allow-list. A node that names a widget
    /// is never treated as a generic block fallback (even if <c>type</c> is set).
    /// </summary>
    private static bool ShouldKeep(HprpLayoutNode node, IReadOnlySet<string> allowed)
    {
        if (!string.IsNullOrWhiteSpace(node.Widget))
        {
            var id = node.Widget.Trim();
            return HprpWidgetIds.All.Contains(id) && allowed.Contains(id);
        }

        return IsGenericBlock(node);
    }
}
