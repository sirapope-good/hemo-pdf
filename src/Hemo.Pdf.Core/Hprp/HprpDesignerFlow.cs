using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Packs designer elements into the content box (parity with Studio <c>reflowElements</c>).
/// </summary>
public static class HprpDesignerFlow
{
    private const float MinBlockW = 10f;
    private const float MinBlockH = 4f;

    public static IReadOnlyList<HprpDesignerElement> Reflow(
        HprpPage? page,
        IReadOnlyList<HprpDesignerElement> elements,
        float contentWidthMm,
        float marginLeftMm = 2f,
        float fallbackSpacingMm = 2f)
    {
        if (elements.Count == 0)
            return elements;

        var gaps = HprpPageLayout.ResolveDesignerGaps(page, marginLeftMm, fallbackSpacingMm);
        var contentW = Math.Max(MinBlockW, contentWidthMm);
        var result = new List<HprpDesignerElement>(elements.Count);
        var cursorY = 0f;
        var i = 0;

        while (i < elements.Count)
        {
            var row = new List<HprpDesignerElement> { elements[i] };
            var j = i + 1;
            while (j < elements.Count
                   && string.Equals(elements[j].Place, "beside", StringComparison.OrdinalIgnoreCase))
            {
                row.Add(elements[j]);
                j++;
            }

            var gapTotal = gaps.BesideMm * Math.Max(0, row.Count - 1);
            // Collapse overlap reduces total span when beside gap is 0.
            if (gaps.BesideMm <= 0 && row.Count > 1)
                gapTotal = -HprpDesignerGaps.BorderCollapseMm * (row.Count - 1);

            var autoCount = row.Count(e => !e.ManualWidth);
            var fixedW = 0f;
            foreach (var e in row)
            {
                if (!e.ManualWidth)
                    continue;
                var w = Math.Clamp(e.Box.WMm > 0 ? e.Box.WMm : MinBlockW, MinBlockW, contentW);
                fixedW += w;
            }

            var remain = Math.Max(MinBlockW * autoCount, contentW - fixedW - gapTotal);
            var autoW = autoCount > 0 ? remain / autoCount : 0f;

            var maxH = 0f;
            var x = 0f;
            var placed = new List<(HprpDesignerElement Src, float X, float Y, float W, float H)>();
            foreach (var e in row)
            {
                var w = e.ManualWidth
                    ? Math.Clamp(e.Box.WMm > 0 ? e.Box.WMm : MinBlockW, MinBlockW, contentW)
                    : Math.Max(MinBlockW, autoW);
                var h = Math.Max(MinBlockH, e.Box.HMm > 0 ? e.Box.HMm : MinBlockH);
                placed.Add((e, x, cursorY, w, h));
                x += gaps.StepX(w);
                maxH = Math.Max(maxH, h);
            }

            foreach (var p in placed)
            {
                result.Add(p.Src.WithBox(new HprpDesignerBox
                {
                    XMm = p.X,
                    YMm = p.Y,
                    WMm = p.W,
                    HMm = maxH,
                }));
            }

            cursorY += gaps.StepY(maxH);
            i = j;
        }

        return result;
    }
}
