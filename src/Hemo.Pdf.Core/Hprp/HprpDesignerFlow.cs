using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>One physical page worth of placed designer elements (absolute coords from page top-left content origin = 0,0 inside margins).</summary>
public sealed class HprpDesignerPageSlice
{
    public int PageIndex { get; init; }
    public IReadOnlyList<HprpDesignerElement> Elements { get; init; } = [];
}

/// <summary>
/// Band-aware reflow + page slicing (parity with Studio).
/// Chrome bands (super-header/header/footer/super-footer) repeat; content flows across pages.
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
        float fallbackSpacingMm = 2f) =>
        ReflowDetailed(page, elements, contentWidthMm, pageHeightMm: 297f, marginTopMm: 2f, marginBottomMm: 2f, marginLeftMm, fallbackSpacingMm)
            .FlatElements;

    public static HprpDesignerFlowResult ReflowDetailed(
        HprpPage? page,
        IReadOnlyList<HprpDesignerElement> elements,
        float contentWidthMm,
        float pageHeightMm,
        float marginTopMm,
        float marginBottomMm,
        float marginLeftMm = 2f,
        float fallbackSpacingMm = 2f)
    {
        if (elements.Count == 0)
        {
            return new HprpDesignerFlowResult
            {
                FlatElements = elements,
                Pages = [new HprpDesignerPageSlice { PageIndex = 0, Elements = [] }],
                ContentFlowHeightMm = Math.Max(MinBlockH, pageHeightMm - marginTopMm - marginBottomMm),
                PageCount = 1,
            };
        }

        var gaps = HprpPageLayout.ResolveDesignerGaps(page, marginLeftMm, fallbackSpacingMm);
        var contentW = Math.Max(MinBlockW, contentWidthMm);

        var superHeader = PackBand(FilterBand(elements, HprpDesignerBands.SuperHeader), contentW, gaps);
        var header = PackBand(FilterBand(elements, HprpDesignerBands.Header), contentW, gaps);
        var footer = PackBand(FilterBand(elements, HprpDesignerBands.Footer), contentW, gaps);
        var superFooter = PackBand(FilterBand(elements, HprpDesignerBands.SuperFooter), contentW, gaps);
        var contentSrc = FilterBand(elements, HprpDesignerBands.Content);

        var chromeTop = BandHeight(superHeader) + BandHeight(header);
        var chromeBottom = BandHeight(footer) + BandHeight(superFooter);
        var contentFlowH = Math.Max(MinBlockH, pageHeightMm - marginTopMm - marginBottomMm - chromeTop - chromeBottom);

        var contentPages = PackContentAcrossPages(contentSrc, contentW, gaps, contentFlowH);

        var pageCount = Math.Max(1, contentPages.Count);
        var pages = new List<HprpDesignerPageSlice>(pageCount);

        for (var p = 0; p < pageCount; p++)
        {
            var pageEls = new List<HprpDesignerElement>();
            var yBase = 0f;

            PlaceBand(superHeader, yBase, pageEls);
            yBase += BandHeight(superHeader);
            PlaceBand(header, yBase, pageEls);
            yBase += BandHeight(header);

            if (p < contentPages.Count)
            {
                foreach (var e in contentPages[p])
                {
                    pageEls.Add(e.WithBox(new HprpDesignerBox
                    {
                        XMm = e.Box.XMm,
                        YMm = yBase + e.Box.YMm,
                        WMm = e.Box.WMm,
                        HMm = e.Box.HMm,
                    }));
                }
            }

            var footerY = pageHeightMm - marginTopMm - marginBottomMm - chromeBottom;
            PlaceBand(footer, footerY, pageEls);
            PlaceBand(superFooter, footerY + BandHeight(footer), pageEls);

            pages.Add(new HprpDesignerPageSlice { PageIndex = p, Elements = pageEls });
        }

        var flat = BuildFlatList(superHeader, header, contentPages, footer, superFooter, contentFlowH);

        return new HprpDesignerFlowResult
        {
            FlatElements = flat,
            Pages = pages,
            ContentFlowHeightMm = contentFlowH,
            SuperHeaderHeightMm = BandHeight(superHeader),
            HeaderHeightMm = BandHeight(header),
            FooterHeightMm = BandHeight(footer),
            SuperFooterHeightMm = BandHeight(superFooter),
            PageCount = pageCount,
        };
    }

    private static List<HprpDesignerElement> BuildFlatList(
        List<HprpDesignerElement> superHeader,
        List<HprpDesignerElement> header,
        List<List<HprpDesignerElement>> contentPages,
        List<HprpDesignerElement> footer,
        List<HprpDesignerElement> superFooter,
        float contentFlowH)
    {
        var flat = new List<HprpDesignerElement>();
        var y = 0f;
        foreach (var e in superHeader)
            flat.Add(OffsetY(e, y));
        y += BandHeight(superHeader);
        foreach (var e in header)
            flat.Add(OffsetY(e, y));
        y += BandHeight(header);

        for (var p = 0; p < contentPages.Count; p++)
        {
            var pageY = y + p * contentFlowH;
            foreach (var e in contentPages[p])
                flat.Add(OffsetY(e, pageY));
        }

        // Footer chrome kept at end of flat with y after last content page chrome zone
        // (Studio uses Pages for multi-sheet; flat is for legacy single sheet / box persistence)
        var afterContent = y + Math.Max(1, contentPages.Count) * contentFlowH;
        foreach (var e in footer)
            flat.Add(OffsetY(e, afterContent));
        afterContent += BandHeight(footer);
        foreach (var e in superFooter)
            flat.Add(OffsetY(e, afterContent));

        return flat;
    }

    private static HprpDesignerElement OffsetY(HprpDesignerElement e, float yBase) =>
        e.WithBox(new HprpDesignerBox
        {
            XMm = e.Box.XMm,
            YMm = yBase + e.Box.YMm,
            WMm = e.Box.WMm,
            HMm = e.Box.HMm,
        });

    private static void PlaceBand(
        List<HprpDesignerElement> band,
        float yBase,
        List<HprpDesignerElement> pageEls)
    {
        foreach (var e in band)
            pageEls.Add(OffsetY(e, yBase));
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
                // Single row taller than page — force place to avoid infinite loop
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

            var maxH = 0f;
            foreach (var e in row)
            {
                var h = Math.Max(
                    MinHeightFor(e),
                    e.Box.HMm > 0 ? e.Box.HMm : MinHeightFor(e));
                maxH = Math.Max(maxH, h);
            }

            if (cursorY + maxH > maxHeight + 0.01f && result.Count > 0)
                break;

            var x = 0f;
            foreach (var e in row)
            {
                var w = e.ManualWidth
                    ? Math.Clamp(e.Box.WMm > 0 ? e.Box.WMm : MinBlockW, MinBlockW, contentW)
                    : Math.Max(MinBlockW, autoW);
                result.Add(e.WithBox(new HprpDesignerBox
                {
                    XMm = x,
                    YMm = cursorY,
                    WMm = w,
                    HMm = maxH,
                }));
                x += gaps.StepX(w);
            }

            cursorY += gaps.StepY(maxH);
            i = j;
        }

        return (result, i);
    }

    private static float MinHeightFor(HprpDesignerElement e) =>
        string.Equals(e.Type, HprpDesignerElementTypes.BoxText, StringComparison.OrdinalIgnoreCase)
            ? MinBoxTextH
            : MinBlockH;
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
    public int PageCount { get; init; } = 1;
}
