using System.Text.Json;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>One physical page of placed designer elements (page-absolute mm from top-left of the sheet).</summary>
public sealed class HprpDesignerPageSlice
{
    public int PageIndex { get; init; }
    public IReadOnlyList<HprpDesignerElement> Elements { get; init; } = [];
}

/// <summary>
/// Band-aware reflow + page slicing (parity with Studio).
/// Super-header / super-footer sit <b>outside</b> the margin guide (in the margin gutter).
/// Header / content / footer flow inside the guide; content may span pages.
/// Element boxes on pages are page-absolute (0,0 = sheet top-left).
/// </summary>
public static class HprpDesignerFlow
{
    private const float MinBlockW = 10f;
    private const float MinBlockH = 4f;
    private const float MinBoxTextH = 3f;

    public static IReadOnlyList<HprpDesignerElement> Reflow(
        HprpPage? page,
        IReadOnlyList<HprpDesignerElement> elements,
        float contentWidthMm,
        float marginLeftMm = 2f,
        float fallbackSpacingMm = 2f,
        JsonElement? data = null,
        object? boundModel = null) =>
        ReflowDetailed(
                page,
                elements,
                contentWidthMm,
                pageHeightMm: 297f,
                marginTopMm: 2f,
                marginBottomMm: 2f,
                marginLeftMm,
                fallbackSpacingMm,
                data,
                boundModel)
            .FlatElements;

    public static HprpDesignerFlowResult ReflowDetailed(
        HprpPage? page,
        IReadOnlyList<HprpDesignerElement> elements,
        float contentWidthMm,
        float pageHeightMm,
        float marginTopMm,
        float marginBottomMm,
        float marginLeftMm = 2f,
        float fallbackSpacingMm = 2f,
        JsonElement? data = null,
        object? boundModel = null)
    {
        var flowElements = HprpDesignerOmit.FilterForFlow(elements, data, boundModel);

        if (flowElements.Count == 0)
        {
            return new HprpDesignerFlowResult
            {
                FlatElements = flowElements,
                Pages = [new HprpDesignerPageSlice { PageIndex = 0, Elements = [] }],
                ContentFlowHeightMm = Math.Max(MinBlockH, pageHeightMm - marginTopMm - marginBottomMm),
                GuideTopMm = marginTopMm,
                GuideHeightMm = Math.Max(MinBlockH, pageHeightMm - marginTopMm - marginBottomMm),
                PageCount = 1,
            };
        }

        var gaps = HprpPageLayout.ResolveDesignerGaps(page, marginLeftMm, fallbackSpacingMm);
        var contentW = Math.Max(MinBlockW, contentWidthMm);

        var superHeader = PackBand(FilterBand(flowElements, HprpDesignerBands.SuperHeader), contentW, gaps);
        var header = PackBand(FilterBand(flowElements, HprpDesignerBands.Header), contentW, gaps);
        var footer = PackBand(FilterBand(flowElements, HprpDesignerBands.Footer), contentW, gaps);
        var superFooter = PackBand(FilterBand(flowElements, HprpDesignerBands.SuperFooter), contentW, gaps);
        var contentSrc = FilterBand(flowElements, HprpDesignerBands.Content);

        var sh = BandHeight(superHeader);
        var sf = BandHeight(superFooter);
        var headerH = BandHeight(header);
        var footerH = BandHeight(footer);

        // Supers live outside the margin guide; guide expands if super taller than margin.
        var guideTop = Math.Max(marginTopMm, sh);
        var guideBottomPad = Math.Max(marginBottomMm, sf);
        var guideHeight = Math.Max(MinBlockH, pageHeightMm - guideTop - guideBottomPad);
        var contentFlowH = Math.Max(MinBlockH, guideHeight - headerH - footerH);

        var contentPages = PackContentAcrossPages(contentSrc, contentW, gaps, contentFlowH);
        // Drop trailing pages that have no content (chrome-only leftovers).
        while (contentPages.Count > 1 && contentPages[^1].Count == 0)
            contentPages.RemoveAt(contentPages.Count - 1);
        var pageCount = Math.Max(1, contentPages.Count);
        var pages = new List<HprpDesignerPageSlice>(pageCount);

        for (var p = 0; p < pageCount; p++)
        {
            var pageEls = new List<HprpDesignerElement>();

            // Super-header: just above the margin guide (outside dashed box).
            AppendExpandedBand(superHeader, marginLeftMm, guideTop - sh, pageEls);

            var innerY = guideTop;
            AppendExpandedBand(header, marginLeftMm, innerY, pageEls);
            innerY += headerH;

            if (p < contentPages.Count)
            {
                foreach (var e in contentPages[p])
                    AppendExpanded(e, marginLeftMm, innerY, pageEls);
            }

            var footerY = guideTop + guideHeight - footerH;
            AppendExpandedBand(footer, marginLeftMm, footerY, pageEls);

            // Super-footer: just below the margin guide (outside dashed box).
            AppendExpandedBand(superFooter, marginLeftMm, guideTop + guideHeight, pageEls);

            pages.Add(new HprpDesignerPageSlice { PageIndex = p, Elements = pageEls });
        }

        var flat = pages.Count > 0 ? pages[0].Elements : Array.Empty<HprpDesignerElement>();

        return new HprpDesignerFlowResult
        {
            FlatElements = flat,
            Pages = pages,
            ContentFlowHeightMm = contentFlowH,
            SuperHeaderHeightMm = sh,
            HeaderHeightMm = headerH,
            FooterHeightMm = footerH,
            SuperFooterHeightMm = sf,
            GuideTopMm = guideTop,
            GuideHeightMm = guideHeight,
            PageCount = pageCount,
        };
    }

    private static void AppendExpandedBand(
        List<HprpDesignerElement> band,
        float originX,
        float originY,
        List<HprpDesignerElement> pageEls)
    {
        foreach (var e in band)
            AppendExpanded(e, originX, originY, pageEls);
    }

    /// <summary>
    /// Emit paintables: groups expand to children (page-absolute).
    /// When the group has a visible <c>chrome.border</c>, the group itself is also emitted
    /// so PDF can draw an outer frame while children keep <c>border: none</c>.
    /// Frame is appended <b>after</b> children so paint order keeps the stroke on top
    /// (otherwise child white fills cover the border and leave broken edge fragments in row gaps).
    /// </summary>
    private static void AppendExpanded(
        HprpDesignerElement e,
        float originX,
        float originY,
        List<HprpDesignerElement> pageEls)
    {
        if (IsGroup(e))
        {
            var gx = originX + e.Box.XMm;
            var gy = originY + e.Box.YMm;

            foreach (var child in e.Children ?? [])
                AppendExpanded(child, gx, gy, pageEls);

            // After children — DesignerPageComposer paints layers in list order.
            if (HasPrintBorder(e.Chrome))
            {
                pageEls.Add(e.WithBox(new HprpDesignerBox
                {
                    XMm = gx,
                    YMm = gy,
                    WMm = e.Box.WMm,
                    HMm = e.Box.HMm,
                }));
            }

            return;
        }

        pageEls.Add(e.WithBox(new HprpDesignerBox
        {
            XMm = originX + e.Box.XMm,
            YMm = originY + e.Box.YMm,
            WMm = e.Box.WMm,
            HMm = e.Box.HMm,
        }));
    }

    private static bool HasPrintBorder(HprpChrome? chrome)
    {
        var b = chrome?.Border?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(b) && b is not "none";
    }

    private static List<HprpDesignerElement> FilterBand(
        IReadOnlyList<HprpDesignerElement> elements,
        string band)
    {
        var want = band.ToLowerInvariant();
        return elements.Where(e => HprpDesignerBands.Resolve(e) == want).ToList();
    }

    private static float BandHeight(List<HprpDesignerElement> band)
    {
        if (band.Count == 0)
            return 0f;
        return band.Max(e => e.Box.YMm + e.Box.HMm);
    }

    private static List<HprpDesignerElement> PackBand(
        List<HprpDesignerElement> source,
        float contentW,
        HprpDesignerGaps gaps)
    {
        if (source.Count == 0)
            return [];
        return PackRows(source, contentW, gaps, maxHeight: float.MaxValue, startY: 0f).rows;
    }

    private static List<List<HprpDesignerElement>> PackContentAcrossPages(
        List<HprpDesignerElement> source,
        float contentW,
        HprpDesignerGaps gaps,
        float contentFlowH)
    {
        if (source.Count == 0)
            return [[]];

        var pages = new List<List<HprpDesignerElement>>();
        var remaining = source.ToList();
        while (remaining.Count > 0)
        {
            var (rows, consumed) = PackRows(remaining, contentW, gaps, contentFlowH, startY: 0f);
            if (consumed == 0)
            {
                var forced = PackRows(remaining.Take(1).ToList(), contentW, gaps, float.MaxValue, 0f).rows;
                pages.Add(forced);
                remaining.RemoveAt(0);
                continue;
            }

            pages.Add(rows);
            remaining = remaining.Skip(consumed).ToList();
        }

        return pages.Count == 0 ? [[]] : pages;
    }

    private static (List<HprpDesignerElement> rows, int consumed) PackRows(
        List<HprpDesignerElement> source,
        float contentW,
        HprpDesignerGaps gaps,
        float maxHeight,
        float startY)
    {
        var result = new List<HprpDesignerElement>();
        var cursorY = startY;
        var i = 0;
        while (i < source.Count)
        {
            var row = new List<HprpDesignerElement> { source[i] };
            var j = i + 1;
            while (j < source.Count
                   && string.Equals(source[j].Place, "beside", StringComparison.OrdinalIgnoreCase))
            {
                row.Add(source[j]);
                j++;
            }

            var gapTotal = gaps.BesideMm * Math.Max(0, row.Count - 1);
            if (gaps.BesideMm <= 0 && row.Count > 1)
                gapTotal = -HprpDesignerGaps.BorderCollapseMm * (row.Count - 1);

            var autoCount = row.Count(e => !e.ManualWidth);
            var fixedW = 0f;
            foreach (var e in row)
            {
                if (!e.ManualWidth)
                    continue;
                fixedW += Math.Clamp(e.Box.WMm > 0 ? e.Box.WMm : MinBlockW, MinBlockW, contentW);
            }

            var remain = Math.Max(MinBlockW * autoCount, contentW - fixedW - gapTotal);
            var autoW = autoCount > 0 ? remain / autoCount : 0f;

            // Pre-measure heights (groups pack children) for page-break decision.
            var measured = new List<HprpDesignerElement>(row.Count);
            var maxH = 0f;
            foreach (var e in row)
            {
                var w = e.ManualWidth
                    ? Math.Clamp(e.Box.WMm > 0 ? e.Box.WMm : MinBlockW, MinBlockW, contentW)
                    : Math.Max(MinBlockW, autoW);
                var packed = IsGroup(e) ? PackColumnGroup(e, w, gaps) : MeasureLeaf(e, w);
                measured.Add(packed);
                maxH = Math.Max(maxH, packed.Box.HMm);
            }

            if (cursorY + maxH > maxHeight + 0.01f && result.Count > 0)
                break;

            var x = 0f;
            foreach (var e in measured)
            {
                var box = new HprpDesignerBox
                {
                    XMm = x,
                    YMm = cursorY,
                    WMm = e.Box.WMm,
                    HMm = e.Box.HMm,
                };
                result.Add(IsGroup(e)
                    ? e.WithBoxAndChildren(box, e.Children ?? Array.Empty<HprpDesignerElement>())
                    : e.WithBox(box));
                x += gaps.StepX(e.Box.WMm);
            }

            cursorY += gaps.StepY(maxH);
            i = j;
        }

        return (result, i);
    }

    private static HprpDesignerElement MeasureLeaf(HprpDesignerElement e, float width)
    {
        var h = Math.Max(
            MinHeightFor(e),
            e.Box.HMm > 0 ? e.Box.HMm : MinHeightFor(e));
        return e.WithBox(new HprpDesignerBox
        {
            XMm = 0,
            YMm = 0,
            WMm = width,
            HMm = h,
        });
    }

    /// <summary>Stack children vertically inside <paramref name="width"/>; child boxes are group-relative.</summary>
    private static HprpDesignerElement PackColumnGroup(
        HprpDesignerElement group,
        float width,
        HprpDesignerGaps gaps)
    {
        var raw = (group.Children ?? Array.Empty<HprpDesignerElement>())
            .Take(HprpDesignerGroupLimits.MaxChildren)
            .ToList();
        if (raw.Count == 0)
        {
            return group.WithBoxAndChildren(
                new HprpDesignerBox { XMm = 0, YMm = 0, WMm = width, HMm = MinBlockH },
                Array.Empty<HprpDesignerElement>());
        }

        var packedKids = new List<HprpDesignerElement>(raw.Count);
        var y = 0f;
        for (var i = 0; i < raw.Count; i++)
        {
            var child = raw[i];
            // Nested groups not supported in v1 — treat as leaf height.
            var leaf = IsGroup(child)
                ? PackColumnGroup(child, width, gaps)
                : MeasureLeaf(child, width);
            packedKids.Add(leaf.WithBox(new HprpDesignerBox
            {
                XMm = 0,
                YMm = y,
                WMm = width,
                HMm = leaf.Box.HMm,
            }));
            if (i < raw.Count - 1)
                y += gaps.StepY(leaf.Box.HMm);
            else
                y += leaf.Box.HMm;
        }

        return group.WithBoxAndChildren(
            new HprpDesignerBox { XMm = 0, YMm = 0, WMm = width, HMm = y },
            packedKids);
    }

    private static bool IsGroup(HprpDesignerElement e) =>
        string.Equals(e.Type, HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase);

    private static float MinHeightFor(HprpDesignerElement e)
    {
        var type = e.Type?.Trim() ?? "";
        if (string.Equals(type, HprpDesignerElementTypes.BoxText, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, HprpDesignerElementTypes.PageOf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, HprpDesignerElementTypes.Narrative, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, HprpDesignerElementTypes.FieldRow, StringComparison.OrdinalIgnoreCase))
        {
            return MinBoxTextH;
        }

        if (IsGroup(e))
            return MinBlockH;

        return MinBlockH;
    }
}

public sealed class HprpDesignerFlowResult
{
    public IReadOnlyList<HprpDesignerElement> FlatElements { get; init; } = [];
    public IReadOnlyList<HprpDesignerPageSlice> Pages { get; init; } = [];
    public float ContentFlowHeightMm { get; init; }
    public float SuperHeaderHeightMm { get; init; }
    public float HeaderHeightMm { get; init; }
    public float FooterHeightMm { get; init; }
    public float SuperFooterHeightMm { get; init; }
    /// <summary>Top of the margin guide (page-absolute).</summary>
    public float GuideTopMm { get; init; }
    /// <summary>Height of the margin guide (inner header/content/footer).</summary>
    public float GuideHeightMm { get; init; }
    public int PageCount { get; init; } = 1;
}
