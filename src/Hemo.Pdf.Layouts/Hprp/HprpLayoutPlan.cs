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
        var templateId = ClinicalReportCatalog.ResolveEngineTemplateId(context.ReportTemplateId);
        return store?.TryGetCached(context.TenantCode, templateId);
    }

    /// <summary>
    /// Ordered widgets from <c>layout.header</c> then <c>layout.body</c>.
    /// Empty / unknown packages return <paramref name="defaults"/>.
    /// </summary>
    public static IReadOnlyList<string> ResolveWidgetOrder(
        HprpPackage? package,
        IReadOnlyList<string> defaults,
        IReadOnlySet<string>? allowed = null)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        if (defaults.Count == 0)
            throw new ArgumentException("defaults must not be empty.", nameof(defaults));

        var allow = allowed ?? ToAllowSet(defaults);
        if (package is null)
            return defaults;

        var order = new List<string>();
        AppendWidget(order, package.Layout.Header?.Widget, allow);
        foreach (var node in package.Layout.Body)
            AppendWidget(order, node.Widget, allow);

        return order.Count > 0 ? order : defaults;
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

        var allow = allowed ?? ToAllowSet(defaults);
        if (package is null)
            return defaults;

        var order = new List<string>();
        foreach (var node in package.Layout.Body)
            AppendWidget(order, node.Widget, allow);

        return order.Count > 0 ? order : defaults;
    }

    private static HashSet<string> ToAllowSet(IReadOnlyList<string> defaults) =>
        new(defaults, StringComparer.OrdinalIgnoreCase);

    private static void AppendWidget(List<string> order, string? widget, IReadOnlySet<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(widget))
            return;

        var id = widget.Trim();
        if (!HprpWidgetIds.All.Contains(id) || !allowed.Contains(id))
            return;

        order.Add(id);
    }
}
