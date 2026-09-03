/**
 * HPRP Studio — WYSIWYG page canvas.
 * Flow layout (no overlap), page margins/border, delete, drag beside/below,
 * resize block edges + column dividers.
 */
(function (global) {
  const DISPLAY_W = 520;
  const A4_W = 210;
  const A4_H = 297;
  const MIN_BLOCK_W = 20;
  const MIN_BLOCK_H = 12;
  /** Banner / single-line box-text can be much shorter than tables. */
  const MIN_BOX_TEXT_H = 4;
  const MIN_COL_WEIGHT = 0.25;
  /** Max children in one column stack (inner section). */
  const MAX_GROUP_CHILDREN = 4;

  function isGroup(el) {
    return String(el && el.type || "").toLowerCase() === "group";
  }

  function resolveBand(el) {
    const b = String(el.band || "").toLowerCase().trim();
    if (BANDS.indexOf(b) >= 0) return b;
    const t = String(el.type || "").toLowerCase();
    if (t === "header") return "header";
    if (t === "page-of") return "super-footer";
    if (t === "group") return "content";
    return "content";
  }

  function minHeightForElement(el) {
    const t = String(el && el.type || "").toLowerCase();
    return t === "box-text" || t === "page-of" || t === "narrative" ? MIN_BOX_TEXT_H : MIN_BLOCK_H;
  }

  /** Locate element in top-level list or inside a group.children. */
  function findElementLocation(id) {
    ensureElements();
    const els = stateRef.draft.layout.elements;
    for (let i = 0; i < els.length; i++) {
      const e = els[i];
      if (e.id === id) return { list: els, index: i, parent: null, el: e };
      if (isGroup(e) && Array.isArray(e.children)) {
        for (let c = 0; c < e.children.length; c++) {
          if (e.children[c].id === id)
            return { list: e.children, index: c, parent: e, el: e.children[c] };
        }
      }
    }
    return null;
  }

  function findElementById(id) {
    const loc = findElementLocation(id);
    return loc ? loc.el : null;
  }

  /** Flatten leaf ids in document order (groups expand). */
  function flattenElementIds(list) {
    const out = [];
    (list || []).forEach((e) => {
      if (isGroup(e)) (e.children || []).forEach((c) => out.push(c.id));
      else out.push(e.id);
    });
    return out;
  }

  function packColumnGroup(group, widthMm, gaps, scale) {
    const kids = (group.children || []).slice(0, MAX_GROUP_CHILDREN);
    group.children = kids;
    group.direction = group.direction || "column";
    if (!kids.length) {
      group.box = group.box || {};
      group.box.wMm = widthMm;
      group.box.hMm = MIN_BLOCK_H;
      return group;
    }
    let y = 0;
    kids.forEach((child, idx) => {
      child.box = child.box || { xMm: 0, yMm: 0, wMm: widthMm, hMm: minHeightForElement(child) };
      const minH = minHeightForElement(child);
      child.box.hMm = Math.max(minH, Number(child.box.hMm) || minH);
      child.box.wMm = widthMm;
      child.box.xMm = 0;
      child.box.yMm = y;
      if (idx < kids.length - 1) y += gapStep(child.box.hMm, gaps.below, scale);
      else y += child.box.hMm;
    });
    group.box = group.box || {};
    group.box.wMm = widthMm;
    group.box.hMm = y;
    return group;
  }

  function measurePacked(el, widthMm, gaps, scale) {
    if (isGroup(el)) return packColumnGroup(el, widthMm, gaps, scale);
    el.box = el.box || { xMm: 0, yMm: 0, wMm: widthMm, hMm: minHeightForElement(el) };
    const minH = minHeightForElement(el);
    el.box.hMm = Math.max(minH, Number(el.box.hMm) || minH);
    el.box.wMm = widthMm;
    return el;
  }

  function expandPageItems(e, originX, originY, outsideMargin) {
    const items = [];
    if (isGroup(e)) {
      const gx = originX + (Number(e.box.xMm) || 0);
      const gy = originY + (Number(e.box.yMm) || 0);
      items.push({
        kind: "group-frame",
        el: e,
        xMm: gx,
        yMm: gy,
        wMm: e.box.wMm,
        hMm: e.box.hMm,
        outsideMargin: !!outsideMargin,
      });
      const kids = e.children || [];
      kids.forEach((child, idx) => {
        items.push({
          kind: "element",
          el: child,
          xMm: gx + (Number(child.box.xMm) || 0),
          yMm: gy + (Number(child.box.yMm) || 0),
          wMm: child.box.wMm,
          hMm: child.box.hMm,
          outsideMargin: !!outsideMargin,
          groupId: e.id,
        });
        if (idx < kids.length - 1) {
          const topChild = kids[idx];
          const splitY = gy + (Number(topChild.box.yMm) || 0) + (Number(topChild.box.hMm) || 0);
          items.push({
            kind: "stack-split",
            groupId: e.id,
            index: idx,
            xMm: gx,
            yMm: splitY,
            wMm: e.box.wMm,
            outsideMargin: !!outsideMargin,
          });
        }
      });
      return items;
    }
    items.push({
      kind: "element",
      el: e,
      xMm: originX + (Number(e.box.xMm) || 0),
      yMm: originY + (Number(e.box.yMm) || 0),
      wMm: e.box.wMm,
      hMm: e.box.hMm,
      outsideMargin: !!outsideMargin,
    });
    return items;
  }

  const CLINICAL01_ANNUAL_BINDINGS = [
    { path: "months[].monthLabel", column: "month", context: "group-label" },
    { path: "months[].entries[].dayLabel", column: "day", context: "entry" },
    { path: "months[].entries[].hb", column: "hb", context: "entry" },
    { path: "months[].entries[].hct", column: "hct", context: "entry" },
    { path: "months[].entries[].epoName", column: "epoName", context: "entry" },
    { path: "months[].entries[].frequencyText", column: "frequencyText", context: "entry" },
    { path: "months[].entries[].injectionDate", column: "injectionDate", context: "entry" },
    { path: "months[].entries[].remarks", column: "remarks", context: "entry" },
    { path: "months[].entries[].labIsHistorical", column: "lab", context: "lab-historical" },
  ];

  let stateRef;
  let elsRef;
  let apiRef;
  let setStatusRef;
  let schedulePreviewRef;
  let selectedElementId = null;
  /** Multi-select set (layout order when saving fragments). Primary = selectedElementId. */
  let selectedElementIds = new Set();
  let tablePresets = {};
  let headerPresets = {};
  let fragmentPresets = {};
  let adapterSchema = null;
  let sampleData = null;
  let dragState = null;
  let suppressClick = false;
  let canvasTools = null;
  let lastFlow = null;

  const BANDS = ["super-header", "header", "content", "footer", "super-footer"];

  function clearElementSelection() {
    selectedElementId = null;
    selectedElementIds = new Set();
  }

  function setSingleSelection(id) {
    selectedElementId = id || null;
    selectedElementIds = id ? new Set([id]) : new Set();
  }

  function toggleAdditiveSelection(id) {
    if (!id) return;
    if (selectedElementIds.has(id)) {
      selectedElementIds.delete(id);
      if (selectedElementId === id) {
        selectedElementId = selectedElementIds.size
          ? selectedElementIds.values().next().value
          : null;
      }
    } else {
      selectedElementIds.add(id);
      selectedElementId = id;
    }
  }

  /** Selected ids in layout order (groups expand to children). */
  function getSelectedIdsInLayoutOrder() {
    ensureElements();
    return flattenElementIds(stateRef.draft.layout.elements || [])
      .filter((id) => selectedElementIds.has(id));
  }

  function isElementSelected(id) {
    return selectedElementIds.has(id);
  }

  function isStudioCanvas() {
    return !stateRef || stateRef.mode !== "json";
  }

  function isDesignerPackage() {
    return String((stateRef.draft.manifest && stateRef.draft.manifest.layoutMode) || "").toLowerCase() === "designer";
  }

  function ensureElements() {
    if (!stateRef.draft.layout) stateRef.draft.layout = {};
    if (!stateRef.draft.layout.elements) stateRef.draft.layout.elements = [];
    if (!stateRef.draft.layout.page) {
      stateRef.draft.layout.page = { size: "A4", marginMm: 2, spacingMm: 2, border: "none" };
    }
    const page = stateRef.draft.layout.page;
    if (page.marginMm == null) page.marginMm = 2;
    if (page.spacingMm == null) page.spacingMm = 2;
    if (page.border == null) page.border = "none";
  }

  /** Canvas cfg-* borders are 0.5px; convert to mm at current zoom scale. */
  function borderCollapseMm(scale) {
    return 0.5 / Math.max(0.01, scale);
  }

  function resolveDesignerGaps(page, margins) {
    const mode = String(page.spacingMode || "custom").toLowerCase();
    if (mode === "none") return { below: 0, beside: 0 };
    if (mode === "margin") {
      const m = page.marginMm != null ? Number(page.marginMm) : Number(margins.left);
      return { below: m, beside: m };
    }
    const shared = page.spacingMm != null ? Number(page.spacingMm) : 2;
    const below = page.spacingBelowMm != null ? Number(page.spacingBelowMm) : shared;
    const beside = page.spacingBesideMm != null ? Number(page.spacingBesideMm) : shared;
    return { below, beside };
  }

  function gapStep(sizeMm, gapMm, scale) {
    if (gapMm > 0) return sizeMm + gapMm;
    if (gapMm === 0) return sizeMm - borderCollapseMm(scale);
    return sizeMm;
  }

  /** Flow-relative X within the content column (not page-absolute). */
  function contentRelativeX(boxXMm, margins, contentW) {
    const x = Number(boxXMm) || 0;
    if (x >= margins.left - 0.01 && x <= margins.left + contentW + 0.01)
      return Math.max(0, x - margins.left);
    return Math.max(0, x);
  }

  /**
   * Max width for east-resize of el within its beside-row.
   * Manual siblings keep their widths; auto siblings only reserve MIN_BLOCK_W
   * so shrinking a manual block does not permanently lock free space into autos
   * (which previously made maxW == currentW and blocked growing back).
   */
  function maxWidthInRow(el, contentW, margins, gaps, scale) {
    const loc = findElementLocation(el.id);
    if (loc && loc.parent && isGroup(loc.parent)) {
      return Math.max(MIN_BLOCK_W, Number(loc.parent.box && loc.parent.box.wMm) || contentW);
    }
    const target = loc && loc.parent ? loc.parent : el;
    const els = stateRef.draft.layout.elements;
    const idx = els.indexOf(target);
    if (idx < 0) return contentW;

    let start = idx;
    while (start > 0 && String(els[start].place || "below").toLowerCase() === "beside") start--;
    let end = idx;
    while (end + 1 < els.length && String(els[end + 1].place || "below").toLowerCase() === "beside") end++;

    const n = end - start + 1;
    let gapTotal = gaps.beside * Math.max(0, n - 1);
    if (gaps.beside <= 0 && n > 1) gapTotal = -borderCollapseMm(scale) * (n - 1);

    let others = 0;
    for (let i = start; i <= end; i++) {
      if (i === idx) continue;
      const sib = els[i];
      if (sib.manualWidth) {
        others += Math.max(MIN_BLOCK_W, Math.min(Number(sib.box && sib.box.wMm) || MIN_BLOCK_W, contentW));
      } else {
        // Auto columns can shrink — only reserve the floor while resizing this block.
        others += MIN_BLOCK_W;
      }
    }
    return Math.max(MIN_BLOCK_W, contentW - gapTotal - others);
  }

  function pageMetrics() {
    ensureElements();
    const page = stateRef.draft.layout.page;
    const landscape = String(page.orientation || "").toLowerCase() === "landscape";
    const pageW = landscape ? A4_H : A4_W;
    const pageH = landscape ? A4_W : A4_H;
    const u = Number(page.marginMm != null ? page.marginMm : 2);
    const m = page.margin || {};
    const margins = {
      top: m.top != null ? Number(m.top) : u,
      right: m.right != null ? Number(m.right) : u,
      bottom: m.bottom != null ? Number(m.bottom) : u,
      left: m.left != null ? Number(m.left) : u,
    };
    const contentW = Math.max(MIN_BLOCK_W, pageW - margins.left - margins.right);
    const contentH = Math.max(MIN_BLOCK_H, pageH - margins.top - margins.bottom);
    const gaps = resolveDesignerGaps(page, margins);
    const spacing = gaps.below; // legacy alias
    const zoom = canvasTools ? canvasTools.getZoom() : 1;
    const scale = (DISPLAY_W * zoom) / pageW;
    const sheetW = DISPLAY_W * zoom;
    return { page, pageW, pageH, margins, contentW, contentH, spacing, gaps, scale, landscape, sheetW, zoom };
  }

  /**
   * Band-aware pack (parity with HprpDesignerFlow):
   * chrome bands repeat; content flows and may create extra pages.
   * Optional blocks with omitWhenEmpty / checklist text-notes are skipped when sample data is empty
   * so the canvas does not invent a chrome-only trailing page.
   */
  function pathIsTruthy(data, path) {
    if (!data || !path) return false;
    let p = String(path).trim();
    if (p.startsWith("$.")) p = p.slice(2);
    else if (p === "$") return !!data;
    else if (p.startsWith("$")) p = p.slice(1).replace(/^\./, "");
    const parts = p.split(".").filter(Boolean);
    let cur = data;
    for (let i = 0; i < parts.length; i++) {
      if (cur == null || typeof cur !== "object") return false;
      cur = cur[parts[i]];
    }
    if (cur == null) return false;
    if (Array.isArray(cur)) return cur.length > 0;
    if (typeof cur === "boolean") return cur;
    if (typeof cur === "number") return cur !== 0;
    return String(cur).trim() !== "";
  }

  function shouldIncludeInFlow(el, data) {
    if (!el) return false;
    if (data == null) return true;
    const omitPath = el.omitWhenEmpty != null ? String(el.omitWhenEmpty).trim() : "";
    if (omitPath) return pathIsTruthy(data, omitPath);
    const widget = String(el.widget || "").toLowerCase();
    if (String(el.type || "").toLowerCase() === "dense" && widget === "clinical.checklist-text-notes") {
      return pathIsTruthy(data, "$.textNotes");
    }
    return true;
  }

  function reflowElements() {
    ensureElements();
    const { contentW, contentH, gaps, pageH, margins, scale } = pageMetrics();
    const els = stateRef.draft.layout.elements.filter((e) => shouldIncludeInFlow(e, sampleData));
    const collapseMm = borderCollapseMm(scale);

    function filterBand(name) {
      return els.filter((e) => resolveBand(e) === name);
    }

    function packRows(source, maxH) {
      const result = [];
      let cursorY = 0;
      let i = 0;
      let consumed = 0;
      while (i < source.length) {
        const row = [source[i]];
        let j = i + 1;
        while (j < source.length && String(source[j].place || "below").toLowerCase() === "beside") {
          row.push(source[j]);
          j++;
        }
        let gapTotal = gaps.beside * Math.max(0, row.length - 1);
        if (gaps.beside <= 0 && row.length > 1) gapTotal = -collapseMm * (row.length - 1);
        const autoCount = row.filter((e) => !e.manualWidth).length;
        let fixedW = 0;
        row.forEach((e) => {
          e.box = e.box || { xMm: 0, yMm: 0, wMm: contentW, hMm: 40 };
          if (e.manualWidth) {
            e.box.wMm = Math.max(MIN_BLOCK_W, Math.min(Number(e.box.wMm) || MIN_BLOCK_W, contentW));
            fixedW += e.box.wMm;
          }
        });
        const remain = Math.max(MIN_BLOCK_W * autoCount, contentW - fixedW - gapTotal);
        const autoW = autoCount > 0 ? remain / autoCount : 0;
        let maxRowH = 0;
        row.forEach((e) => {
          const w = e.manualWidth
            ? Math.max(MIN_BLOCK_W, Math.min(Number(e.box.wMm) || MIN_BLOCK_W, contentW))
            : Math.max(MIN_BLOCK_W, autoW);
          measurePacked(e, w, gaps, scale);
          maxRowH = Math.max(maxRowH, e.box.hMm);
        });
        if (cursorY + maxRowH > maxH + 0.01 && result.length > 0) break;

        let x = 0;
        row.forEach((e) => {
          e.box.xMm = x;
          e.box.yMm = cursorY;
          x += gapStep(e.box.wMm, gaps.beside, scale);
          result.push(e);
        });
        cursorY += gapStep(maxRowH, gaps.below, scale);
        i = j;
        consumed = i;
      }
      return {
        rows: result,
        consumed,
        height: cursorY > 0 ? cursorY - (gaps.below > 0 ? gaps.below : -collapseMm) : 0,
      };
    }

    function bandHeight(packed) {
      if (!packed.length) return 0;
      return Math.max.apply(null, packed.map((e) => e.box.yMm + e.box.hMm));
    }

    const superHeader = packRows(filterBand("super-header"), 1e9).rows;
    const header = packRows(filterBand("header"), 1e9).rows;
    const footer = packRows(filterBand("footer"), 1e9).rows;
    const superFooter = packRows(filterBand("super-footer"), 1e9).rows;
    const contentSrc = filterBand("content");

    const sh = bandHeight(superHeader);
    const sf = bandHeight(superFooter);
    const headerH = bandHeight(header);
    const footerH = bandHeight(footer);

    // Supers sit outside the margin guide (in the margin gutter / beyond dashed box).
    const guideTop = Math.max(margins.top, sh);
    const guideBottomPad = Math.max(margins.bottom, sf);
    const guideHeight = Math.max(MIN_BLOCK_H, pageH - guideTop - guideBottomPad);
    const contentFlowH = Math.max(MIN_BLOCK_H, guideHeight - headerH - footerH);

    const contentPages = [];
    let remaining = contentSrc.slice();
    while (remaining.length > 0) {
      const packed = packRows(remaining, contentFlowH);
      if (packed.consumed === 0) {
        const forced = packRows(remaining.slice(0, 1), 1e9).rows;
        contentPages.push(forced);
        remaining = remaining.slice(1);
        continue;
      }
      contentPages.push(packed.rows);
      remaining = remaining.slice(packed.consumed);
    }
    if (contentPages.length === 0) contentPages.push([]);
    while (contentPages.length > 1 && contentPages[contentPages.length - 1].length === 0) {
      contentPages.pop();
    }

    const pageCount = Math.max(1, contentPages.length);
    const pages = [];
    for (let p = 0; p < pageCount; p++) {
      const pageEls = [];
      function placeBandAbs(band, originX, originY) {
        band.forEach((e) => {
          const outside = originY < guideTop - 0.01 || originY >= guideTop + guideHeight - 0.01;
          expandPageItems(e, originX, originY, outside).forEach((item) => pageEls.push(item));
        });
      }

      placeBandAbs(superHeader, margins.left, guideTop - sh);
      let innerY = guideTop;
      placeBandAbs(header, margins.left, innerY);
      innerY += headerH;
      (contentPages[p] || []).forEach((e) => {
        expandPageItems(e, margins.left, innerY, false).forEach((item) => pageEls.push(item));
      });
      placeBandAbs(footer, margins.left, guideTop + guideHeight - footerH);
      placeBandAbs(superFooter, margins.left, guideTop + guideHeight);
      pages.push(pageEls);
    }

    // packRows already wrote flow-relative box on each element — do not overwrite with page-absolute coords.

    lastFlow = {
      pages,
      pageCount,
      contentFlowH,
      guideTop,
      guideHeight,
      superHeaderH: sh,
      headerH,
      footerH,
      superFooterH: sf,
      pageH,
      margins,
      contentW,
      contentH,
    };
  }

  function clinical01CopayPieces() {
    const frag = fragmentPresets["copay-duo-v1"];
    if (frag && Array.isArray(frag.elements) && frag.elements.length) {
      return JSON.parse(JSON.stringify(frag.elements));
    }
    return [
      {
        id: "copay-banner",
        type: "box-text",
        band: "content",
        place: "below",
        box: { xMm: 0, yMm: 0, wMm: 206, hMm: 5 },
        text: "ปริมาณยาที่มีสิทธิได้รับโดยไม่ต้องร่วมจ่าย",
        bind: "$.coPayCriteria.title",
        align: "center",
        chrome: { headerFill: "$branding.sectionHeaderBackground", border: "thin", fontSize: 7.5 },
      },
      {
        id: "copay-nhso",
        type: "config-table",
        band: "content",
        presetId: "copay-nhso-v1",
        place: "below",
        manualWidth: true,
        box: { xMm: 0, yMm: 0, wMm: 78, hMm: 27 },
        chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground" },
      },
      {
        id: "copay-sso",
        type: "config-table",
        band: "content",
        presetId: "copay-sso-v1",
        place: "beside",
        manualWidth: true,
        box: { xMm: 0, yMm: 0, wMm: 127, hMm: 27 },
        chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground" },
      },
    ];
  }

  function isLegacyDenseCopay(el) {
    if (!el) return false;
    const widget = String(el.widget || "").toLowerCase();
    const type = String(el.type || "").toLowerCase();
    return widget === "clinical.hct-epo-copay"
      || type === "clinical.hct-epo-copay"
      || (type === "dense" && widget.indexOf("hct-epo-copay") >= 0);
  }

  /** Replace legacy dense clinical.hct-epo-copay with box-text + duo tables. */
  function migrateLegacyDenseCopay() {
    ensureElements();
    const els = stateRef.draft.layout.elements;
    let changed = false;

    // Drop stale dense + any half-migrated ids so we can insert a clean trio once.
    const hasLegacy = els.some(isLegacyDenseCopay);
    if (!hasLegacy) return false;

    const insertAt = Math.max(0, els.findIndex(isLegacyDenseCopay));
    const next = els.filter((e) =>
      !isLegacyDenseCopay(e)
      && e.id !== "copay-banner"
      && e.id !== "copay-nhso"
      && e.id !== "copay-sso"
      && e.id !== "copay");
    const pieces = clinical01CopayPieces();
    next.splice(insertAt, 0, pieces[0], pieces[1], pieces[2]);
    stateRef.draft.layout.elements = next;
    changed = true;
    if (setStatusRef) {
      setStatusRef("แทนที่ dense clinical.hct-epo-copay → box-text + ตาราง NHSO/SSO แล้ว", "ok");
    }
    return changed;
  }

  function clinical05ChecklistMatrixElement(gridChrome) {
    const chrome = Object.assign({
      headerFill: "#E8EEF5",
      border: "thin",
      fontSize: 9,
      columnWidths: ["46mm", "*"],
    }, gridChrome || {});
    if (!chrome.columnWidths) chrome.columnWidths = ["46mm", "*"];
    return {
      id: "grid",
      type: "config-table",
      band: "content",
      presetId: "progress-note-checklist-matrix-v1",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 269, hMm: 118 },
      chrome: chrome,
    };
  }

  /** Upgrade legacy dense clinical.checklist-grid → config-table matrix preset. */
  function migrateDenseChecklistGridToConfigTable() {
    ensureElements();
    const layout = stateRef.draft.layout;
    const els = layout.elements || [];
    const denseIdx = els.findIndex((e) =>
      e
      && e.type === "dense"
      && String(e.widget || "").toLowerCase() === "clinical.checklist-grid");
    if (denseIdx < 0) return false;

    const dense = els[denseIdx];
    const replacement = clinical05ChecklistMatrixElement(dense.chrome);
    replacement.id = dense.id || "grid";
    replacement.box = Object.assign({}, dense.box || replacement.box);
    replacement.place = dense.place || "below";
    replacement.band = dense.band || "content";
    els[denseIdx] = replacement;
    layout.elements = els;
    if (setStatusRef) setStatusRef("อัปเกรด dense checklist-grid → config-table matrix แล้ว", "ok");
    return true;
  }

  function clinical05SoapConfigTableElement(soapChrome) {
    const chrome = Object.assign({
      border: "thin",
      headerFill: "$branding.sectionHeaderBackground",
      bandWeights: [1, 2.5, 1, 1],
    }, soapChrome || {});
    if (!chrome.bandWeights) chrome.bandWeights = [1, 2.5, 1, 1];
    return {
      id: "soap",
      type: "config-table",
      band: "content",
      presetId: "progress-note-soap-v1",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 206, hMm: 255 },
      chrome: chrome,
      bindings: [
        { path: "sessions[].dateLabel", column: "date", context: "freedom-row" },
        { path: "sessions[].orderForOneDay", column: "orderOneDay", context: "freedom-row" },
        { path: "sessions[].orderForContinuation", column: "orderContinuation", context: "freedom-row" },
      ],
    };
  }

  /** Upgrade legacy dense clinical.soap-table → config-table progress-note-soap-v1. */
  function migrateDenseSoapToConfigTable() {
    ensureElements();
    const layout = stateRef.draft.layout;
    const els = layout.elements || [];
    const denseIdx = els.findIndex((e) =>
      e
      && e.type === "dense"
      && String(e.widget || "").toLowerCase() === "clinical.soap-table");
    if (denseIdx < 0) return false;

    const dense = els[denseIdx];
    const replacement = clinical05SoapConfigTableElement(dense.chrome);
    replacement.id = dense.id || "soap";
    replacement.box = Object.assign({}, dense.box || replacement.box);
    replacement.place = dense.place || "below";
    replacement.band = dense.band || "content";
    els[denseIdx] = replacement;
    layout.elements = els;
    if (setStatusRef) setStatusRef("อัปเกรด dense SOAP → config-table progress-note-soap-v1 แล้ว", "ok");
    return true;
  }

  /** Replace checklist title box-texts with clinical-header-thaiur (landscape-aware). */
  function migrateChecklistToThaiUrHeader() {
    const manifest = stateRef.draft.manifest || {};
    const packId = String(manifest.id || "");
    const adapter = String(manifest.dataAdapter || "");
    const isChecklist =
      packId.indexOf("clinical-05-progress-note-checklist") === 0
      || adapter === "clinical-05-progress-note-checklist";
    if (!isChecklist) return false;

    ensureElements();
    const layout = stateRef.draft.layout;
    const els = layout.elements || [];
    const hasThaiUr = els.some((e) =>
      e
      && e.type === "header"
      && (e.preset === "clinical-header-thaiur"
        || (e.headerPreset && e.headerPreset.id === "clinical-header-thaiur")));
    if (hasThaiUr) return false;

    const contentW = String((layout.page && layout.page.orientation) || "").toLowerCase() === "landscape" ? 269 : 206;
    const keepBody = els.filter((e) =>
      e && (e.type === "dense" || e.type === "page-of" || e.type === "config-table"));
    const rangeEl = els.find((e) => e && e.type === "box-text" && e.bind === "$.rangeLabel");
    const codeEl = els.find((e) => e && e.type === "box-text" && e.bind === "$.reportCodeValue");

    const next = [
      {
        id: "hdr",
        type: "header",
        band: "header",
        preset: "clinical-header-thaiur",
        bottomMode: "checklist-patient",
        place: "below",
        box: { xMm: 0, yMm: 0, wMm: contentW, hMm: 32.4 },
      },
      Object.assign({}, codeEl || {}, {
        id: (codeEl && codeEl.id) || "report-code",
        type: "box-text",
        band: "super-header",
        place: "below",
        box: {
          xMm: Math.max(0, contentW - 69),
          yMm: 2,
          wMm: 69,
          hMm: 5,
        },
        bind: "$.reportCodeValue",
        align: "right",
        chrome: { border: "none", headerFill: "#ffffff", fontSize: 9 },
      }),
      Object.assign({}, rangeEl || {}, {
        id: (rangeEl && rangeEl.id) || "range",
        type: "box-text",
        band: "content",
        place: "below",
        box: { xMm: 0, yMm: 0, wMm: contentW, hMm: 5 },
        bind: "$.rangeLabel",
        align: "center",
        chrome: { border: "none", headerFill: "#ffffff", fontSize: 10 },
      }),
    ];

    keepBody.forEach((e) => {
      next.push(Object.assign({}, e, {
        box: Object.assign({}, e.box, { wMm: contentW }),
      }));
    });

    layout.page = Object.assign({
      size: "A4",
      orientation: "landscape",
      marginMm: 14,
      spacingMode: "custom",
      spacingMm: 2,
    }, layout.page || {});
    layout.elements = next;
    if (setStatusRef) setStatusRef("อัปเกรด Default checklist → clinical-header-thaiur (landscape) แล้ว", "ok");
    return true;
  }

  /** Move Default patient KV into ThaiUR header bottomMode=checklist-patient. */
  function migrateChecklistPatientIntoHeaderBottom() {
    const manifest = stateRef.draft.manifest || {};
    const packId = String(manifest.id || "");
    const adapter = String(manifest.dataAdapter || "");
    const isChecklist =
      packId.indexOf("clinical-05-progress-note-checklist") === 0
      || adapter === "clinical-05-progress-note-checklist";
    if (!isChecklist) return false;

    ensureElements();
    const layout = stateRef.draft.layout;
    const els = layout.elements || [];
    const hdr = els.find((e) =>
      e
      && e.type === "header"
      && (e.preset === "clinical-header-thaiur"
        || (e.headerPreset && e.headerPreset.id === "clinical-header-thaiur")));
    if (!hdr) return false;

    const patientIdx = els.findIndex((e) =>
      e
      && e.type === "dense"
      && String(e.widget || "").toLowerCase() === "clinical.checklist-patient");

    const already = String(hdr.bottomMode || "").toLowerCase() === "checklist-patient";
    if (already && patientIdx < 0) return false;

    if (canvasTools) canvasTools.pushHistory();
    hdr.bottomMode = "checklist-patient";
    const titleH = 21.6;
    const bottomH = 10.8;
    hdr.box = Object.assign({}, hdr.box || {}, {
      hMm: titleH + bottomH,
      wMm: hdr.box && hdr.box.wMm ? hdr.box.wMm : 269,
    });
    if (patientIdx >= 0) els.splice(patientIdx, 1);
    layout.elements = els;
    if (setStatusRef) {
      setStatusRef("ย้าย checklist patient → header bottomMode=checklist-patient แล้ว", "ok");
    }
    return true;
  }

  /** Ensure checklist text-notes declare omitWhenEmpty so empty samples do not invent page 2. */
  function migrateOmitWhenEmptyOnChecklistNotes() {
    ensureElements();
    const els = stateRef.draft.layout.elements || [];
    let changed = false;
    els.forEach((e) => {
      if (!e || String(e.type || "").toLowerCase() !== "dense") return;
      if (String(e.widget || "").toLowerCase() !== "clinical.checklist-text-notes") return;
      if (e.omitWhenEmpty != null && String(e.omitWhenEmpty).trim() !== "") return;
      e.omitWhenEmpty = "$.textNotes";
      changed = true;
    });
    return changed;
  }

  function promoteToDesignerIfNeeded() {
    ensureElements();
    const manifest = stateRef.draft.manifest || (stateRef.draft.manifest = {});
    const layout = stateRef.draft.layout;

    if (isDesignerPackage() && layout.elements.length > 0) {
      migrateLegacyDenseCopay();
      migrateChecklistToThaiUrHeader();
      migrateDenseSoapToConfigTable();
      migrateDenseChecklistGridToConfigTable();
      migrateChecklistPatientIntoHeaderBottom();
      migrateOmitWhenEmptyOnChecklistNotes();
      reflowElements();
      return false;
    }

    const body = layout.body || [];
    const packId = String(manifest.id || "");
    const adapter = String(manifest.dataAdapter || "");
    const hasAnnual = body.some((n) => n && n.widget === "clinical.hct-epo-annual-table");
    const isClinical01 =
      packId.indexOf("clinical-01-hct-epo") === 0
      || adapter === "clinical-01-hct-epo"
      || hasAnnual;
    const isProgressChecklist =
      packId.indexOf("clinical-05-progress-note-checklist") === 0
      || adapter === "clinical-05-progress-note-checklist";
    const isProgressSoap =
      !isProgressChecklist
      && (packId.indexOf("clinical-05-progress-note") === 0
        || adapter === "clinical-05-progress-note"
        || body.some((n) => n && n.widget === "clinical.soap-table"));
    const isClinical07 =
      packId.indexOf("clinical-07-lab") === 0
      || adapter === "clinical-07-lab"
      || body.some((n) => n && n.type === "data-grid" && n.bindRows);

    if (isClinical01 && layout.elements.length === 0) {
      layout.elements = [
        {
          id: "hdr",
          type: "header",
          band: "header",
          preset: "clinical-header-thaiur",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 27 },
        },
        {
          id: "annual",
          type: "config-table",
          band: "content",
          presetId: "hct-epo-annual-v1",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 228 },
          bindings: CLINICAL01_ANNUAL_BINDINGS.slice(),
          chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground" },
        },
      ].concat(clinical01CopayPieces());
    } else if (isProgressSoap && layout.elements.length === 0) {
      const soapChrome = (body.find((n) => n && n.widget === "clinical.soap-table") || {}).chrome || {};
      layout.page = Object.assign({ size: "A4", orientation: "portrait", marginMm: 2, spacingMode: "custom", spacingMm: 2 }, layout.page || {});
      layout.elements = [
        {
          id: "hdr",
          type: "header",
          band: "header",
          preset: "clinical-header-thaiur",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 27 },
        },
        clinical05SoapConfigTableElement(soapChrome),
        {
          id: "page-of",
          type: "page-of",
          band: "super-footer",
          place: "below",
          box: { xMm: 2, yMm: 292, wMm: 206, hMm: 5 },
          text: "{current} / {total}",
          align: "center",
          chrome: { border: "none", fontSize: 8 },
        },
      ];
    } else if (isProgressChecklist && layout.elements.length === 0) {
      const gridChrome = (body.find((n) => n && n.widget === "clinical.checklist-grid") || {}).chrome || {
        headerFill: "#E8EEF5",
        border: "thin",
        fontSize: 9,
      };
      layout.page = Object.assign({
        size: "A4",
        orientation: "landscape",
        marginMm: 14,
        spacingMode: "custom",
        spacingMm: 2,
      }, layout.page || {});
      layout.elements = [
        {
          id: "hdr",
          type: "header",
          band: "header",
          preset: "clinical-header-thaiur",
          bottomMode: "checklist-patient",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 269, hMm: 32.4 },
        },
        {
          id: "report-code",
          type: "box-text",
          band: "super-header",
          place: "below",
          box: { xMm: 200, yMm: 2, wMm: 69, hMm: 5 },
          bind: "$.reportCodeValue",
          align: "right",
          chrome: { border: "none", headerFill: "#ffffff", fontSize: 9 },
        },
        {
          id: "range",
          type: "box-text",
          band: "content",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 269, hMm: 5 },
          bind: "$.rangeLabel",
          align: "center",
          chrome: { border: "none", headerFill: "#ffffff", fontSize: 10 },
        },
        clinical05ChecklistMatrixElement(gridChrome),
        {
          id: "text-notes",
          type: "dense",
          band: "content",
          widget: "clinical.checklist-text-notes",
          place: "below",
          omitWhenEmpty: "$.textNotes",
          box: { xMm: 0, yMm: 0, wMm: 269, hMm: 4 },
        },
        {
          id: "page-of",
          type: "page-of",
          band: "super-footer",
          place: "below",
          box: { xMm: 14, yMm: 202, wMm: 269, hMm: 5 },
          text: "Page {current} of {total}",
          align: "right",
          chrome: { border: "none", fontSize: 8 },
        },
      ];
    } else if (isClinical07 && layout.elements.length === 0) {
      const gridNode = body.find((n) => n && n.type === "data-grid") || {};
      const gridChrome = gridNode.chrome || {
        headerFill: "$branding.sectionHeaderBackground",
        border: "thin",
        fontSize: 7,
        columnWidths: ["3", "*", "*", "*", "*", "*", "*"],
      };
      const marginMm = Number((layout.page && layout.page.marginMm) || 8);
      const contentW = Math.max(10, 210 - 2 * marginMm);
      layout.page = Object.assign({
        size: "A4",
        orientation: "portrait",
        marginMm: marginMm,
        spacingMode: "custom",
        spacingMm: 0,
        spacingBelowMm: 0,
        spacingBesideMm: 0,
        border: "none",
      }, layout.page || {});
      layout.elements = [
        {
          id: "hdr",
          type: "header",
          band: "header",
          preset: "clinical-header-thaiur",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: contentW, hMm: 27 },
        },
        {
          id: "lab-grid",
          type: "data-grid",
          band: "content",
          place: "below",
          bindRows: gridNode.bindRows || "$.rows",
          columnHeadersBind: gridNode.columnHeadersBind || "$.columnHeaders",
          box: { xMm: 0, yMm: 0, wMm: contentW, hMm: 254 },
          chrome: JSON.parse(JSON.stringify(gridChrome)),
        },
      ];
    }

    if (layout.elements.length === 0) {
      layout.elements.push({
        id: "tbl_main",
        type: "config-table",
        band: "content",
        presetId: "hct-epo-annual-v1",
        place: "below",
        box: { xMm: 0, yMm: 0, wMm: 206, hMm: 200 },
        bindings: [],
        chrome: { border: "thin" },
      });
    }

    migrateLegacyDenseCopay();
    manifest.layoutMode = "designer";
    reflowElements();
    return true;
  }

  function selectedElement() {
    ensureElements();
    return findElementById(selectedElementId);
  }

  function resolveTablePreset(el) {
    if (el.tablePreset && (el.tablePreset.id || (el.tablePreset.columns && el.tablePreset.columns.length)))
      return el.tablePreset;
    if (el.presetId && tablePresets[el.presetId]) return tablePresets[el.presetId];
    return el.tablePreset || null;
  }

  async function loadCatalogExtras(opts) {
    if (!apiRef) return;
    const silent = opts && opts.silent;
    try {
      const presets = await apiRef("/api/hprp/presets/tables");
      tablePresets = {};
      (presets || []).forEach((p) => { tablePresets[p.id] = p; });
    } catch (_) { /* optional */ }

    try {
      const headers = await apiRef("/api/hprp/presets/headers");
      headerPresets = {};
      (headers || []).forEach((p) => { headerPresets[p.id] = p; });
    } catch (_) { /* optional */ }

    try {
      const frags = await apiRef("/api/hprp/presets/fragments");
      fragmentPresets = {};
      (frags || []).forEach((p) => { fragmentPresets[p.id] = p; });
    } catch (_) { /* optional */ }

    if (!silent && global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
      global.LibraryStudio.refresh();
    }

    const adapter = stateRef && stateRef.draft && stateRef.draft.manifest && stateRef.draft.manifest.dataAdapter;
    if (adapter) {
      try {
        adapterSchema = await apiRef(`/api/hprp/adapters/${encodeURIComponent(adapter)}/schema`);
      } catch (_) {
        adapterSchema = null;
      }
    }
  }

  function resolveHeaderPreset(el) {
    if (el.headerPreset && (el.headerPreset.id || (el.headerPreset.columns && el.headerPreset.columns.length)))
      return el.headerPreset;
    if (el.preset && headerPresets[el.preset]) return headerPresets[el.preset];
    return el.headerPreset || null;
  }

  function ensureWorkingHeaderPreset(el, preset) {
    if (el.headerPreset) return el.headerPreset;
    el.headerPreset = JSON.parse(JSON.stringify(preset));
    if (!el.headerPreset.id) el.headerPreset.id = el.preset || "inline-header";
    delete el.preset;
    return el.headerPreset;
  }

  function commitHeaderWorking(el, working) {
    el.headerPreset = working;
    delete el.preset;
    stateRef.draft.manifest.layoutMode = "designer";
  }

  async function loadSampleData() {
    const item = stateRef.selected;
    if (!item) return;
    const scenarioEl = document.getElementById("designerSampleScenario");
    const scenario = scenarioEl && scenarioEl.value ? scenarioEl.value : "";
    const qs = new URLSearchParams();
    if (item.variant) qs.set("variant", item.variant);
    if (scenario) qs.set("scenario", scenario);
    const q = qs.toString() ? "?" + qs.toString() : "";
    const useClinical01Sample =
      (stateRef.libraryEdit && (stateRef.libraryEdit.kind === "headers"
        || stateRef.libraryEdit.kind === "tables"
        || stateRef.libraryEdit.kind === "fragments"))
      || String(item.id || "").indexOf("__library__/") === 0;
    try {
      if (useClinical01Sample) {
        sampleData = await apiRef(`/api/hprp/packages/clinical-01-hct-epo/sample-data${q}`);
        return;
      }
      sampleData = await apiRef(`/api/hprp/packages/${encodeURIComponent(item.id)}/sample-data${q}`);
    } catch (_) {
      if (useClinical01Sample || (String(item.id).indexOf("clinical-01") === 0 && item.id !== "clinical-01-hct-epo")) {
        try {
          sampleData = await apiRef(`/api/hprp/packages/clinical-01-hct-epo/sample-data${q}`);
        } catch (__) {
          sampleData = null;
        }
      } else sampleData = null;
    }
    sampleData = normalizeSampleForThaiUrHeader(sampleData);
  }

  /** Checklist wire uses top-level patient; ThaiUR header binds $.header.patient.*. */
  function normalizeSampleForThaiUrHeader(data) {
    if (!data || typeof data !== "object") return data;
    if (data.header && data.header.patient) return data;
    const p = data.patient;
    if (!p || typeof p !== "object") return data;
    const next = Object.assign({}, data);
    next.header = Object.assign({}, data.header || {}, {
      patient: {
        name: p.name || "",
        hn: p.hospitalNumber || p.hn || "",
        age: p.age,
        coverage: p.coverageScheme || p.coverage || "",
        identityNumber: p.identityNumber || "",
        diagnosis: p.underlying || p.diagnosis || "",
        allergies: p.allergies || [],
        hdPerWeek: p.sessionsPerWeekLabel || p.hdPerWeek || "",
      },
      unit: (data.header && data.header.unit) || { fullName: "" },
    });
    return next;
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function readJsonPathRaw(data, path) {
    if (!data || !path) return null;
    let p = String(path).trim();
    if (p.startsWith("$.")) p = p.slice(2);
    else if (p.startsWith("$")) p = p.slice(1).replace(/^\./, "");
    const parts = p.split(".").filter(Boolean);
    let cur = data;
    for (let i = 0; i < parts.length; i++) {
      if (cur == null || typeof cur !== "object") return null;
      cur = cur[parts[i]];
    }
    return cur;
  }

  function parseDataGridColumnWeights(columnWidths, columnCount) {
    return resolveDataGridColumnWeights(columnWidths, columnCount);
  }

  function parseDataGridWeightToken(raw, fallback) {
    let token = String(raw == null ? "" : raw).trim();
    if (!token || token === "*") return fallback;
    if (token.toLowerCase().endsWith("mm")) token = token.slice(0, -2).trim();
    const n = Number(token);
    return Number.isFinite(n) && n > 0 ? n : fallback;
  }

  function formatDataGridWeightToken(weight, previousToken) {
    const prev = String(previousToken || "").trim();
    if (prev === "*" && weight >= 0.999 && weight <= 1.001) return "*";
    if (prev.toLowerCase().endsWith("mm")) {
      const n = Math.round(weight * 100) / 100;
      return n.toFixed(2).replace(/\.?0+$/, "") + "mm";
    }
    if (weight >= 0.999 && weight <= 1.001 && (!prev || prev === "*")) return "*";
    return String(Math.round(weight * 100) / 100);
  }

  /** Mirrors HprpDataGridColumnPlan.Resolve (C#). */
  function resolveDataGridColumnWeights(columnWidths, columnCount) {
    if (!columnCount || columnCount <= 0) return [1];
    const tokens = (columnWidths || []).slice();
    if (tokens.length === columnCount) {
      const weights = [];
      for (let i = 0; i < columnCount; i++) {
        weights.push(parseDataGridWeightToken(tokens[i], 1));
      }
      return weights;
    }
    const src = tokens.length ? tokens : ["3", "*"];
    const lab = parseDataGridWeightToken(src[0], 3);
    const date = src.length > 1 ? parseDataGridWeightToken(src[1], 1) : 1;
    const weights = [lab];
    for (let i = 1; i < columnCount; i++) weights.push(date);
    return weights;
  }

  function normalizeDataGridColumnTokens(columnWidths, columnCount) {
    const weights = resolveDataGridColumnWeights(columnWidths, columnCount);
    const src = columnWidths && columnWidths.length ? columnWidths : ["3", "*"];
    const out = [];
    for (let i = 0; i < columnCount; i++) {
      const raw = i < src.length ? src[i] : (i === 0 ? src[0] : (src.length > 1 ? src[1] : "*"));
      out.push(formatDataGridWeightToken(weights[i], raw));
    }
    return out;
  }

  function ensureDataGridColumnWidths(el, columnCount) {
    if (!columnCount || columnCount <= 0) return;
    el.chrome = el.chrome || {};
    el.chrome.columnWidths = normalizeDataGridColumnTokens(el.chrome.columnWidths, columnCount);
  }

  function writeDataGridWeightsToChrome(el, weights) {
    el.chrome = el.chrome || {};
    const prev = el.chrome.columnWidths || [];
    el.chrome.columnWidths = weights.map((w, i) => formatDataGridWeightToken(w, prev[i]));
  }

  const DATA_GRID_EMPTY_DATE_HEADER = "DATE";

  function normalizeDataGridHeaders(headers) {
    if (!headers || !headers.length) return [DATA_GRID_EMPTY_DATE_HEADER];
    const out = headers.map((h) => (h == null ? "" : String(h).trim()));
    if (!out[0] || out[0].toLowerCase() === "content") out[0] = DATA_GRID_EMPTY_DATE_HEADER;
    return out;
  }

  function resolveDataGridModel(el, data) {
    let headers = Array.isArray(el.columnHeaders) ? el.columnHeaders.slice() : [];
    if (!headers.length && el.columnHeadersBind) {
      const bound = readJsonPathRaw(data, el.columnHeadersBind);
      if (Array.isArray(bound)) {
        headers = bound.map((h) => (h == null ? "" : String(h)));
      }
    }
    const rawRows = readJsonPathRaw(data, el.bindRows);
    const rows = [];
    if (Array.isArray(rawRows)) {
      rawRows.forEach((item) => {
        if (Array.isArray(item)) {
          rows.push(item.map((cell) => (cell == null ? "" : String(cell))));
          return;
        }
        if (item != null && typeof item === "object") {
          rows.push(Object.values(item).map((cell) => (cell == null ? "" : String(cell))));
        }
      });
    }
    if (!headers.length && rows.length) {
      headers = rows[0].map((_, i) => (i === 0 ? DATA_GRID_EMPTY_DATE_HEADER : ""));
    }
    headers = normalizeDataGridHeaders(headers);
    return { headers, rows };
  }

  function borderOn(chrome) {
    const b = String((chrome && chrome.border) || "thin").toLowerCase();
    return b !== "none" && b !== "off" && b !== "false";
  }

  function renderCanvas() {
    const host = document.getElementById("designerCanvas");
    if (!host || !isStudioCanvas()) return;
    ensureElements();
    reflowElements();
    host.innerHTML = "";

    const m = pageMetrics();
    const { page, pageH, margins, contentW, scale, landscape, sheetW, gaps } = m;
    const flow = lastFlow || {
      pages: [[]],
      pageCount: 1,
      guideTop: margins.top,
      guideHeight: pageH - margins.top - margins.bottom,
      headerH: 0,
      superHeaderH: 0,
      footerH: 0,
      superFooterH: 0,
      contentFlowH: 0,
    };

    const banner = document.getElementById("designerOverflowBanner");
    if (banner) {
      if (flow.pageCount > 1) {
        banner.classList.remove("hidden");
        banner.textContent =
          "เนื้อหาเกิน 1 หน้า — แสดง " + flow.pageCount + " หน้าอัตโนมัติ (Header/Footer ซ้ำทุกหน้า · Content ไหลต่อ · Super อยู่นอกเส้น margin)";
      } else {
        banner.classList.add("hidden");
        banner.textContent = "";
      }
    }

    const stack = document.createElement("div");
    stack.className = "designer-sheet-stack";

    const lang = Object.keys(stateRef.draft.labels || {})[0] || "th";
    const labels = (stateRef.draft.labels && stateRef.draft.labels[lang]) || {};

    for (let p = 0; p < flow.pageCount; p++) {
      const sheet = document.createElement("div");
      sheet.className = "designer-sheet" + (landscape ? " landscape" : "") + (p > 0 ? " overflow-page" : "");
      sheet.style.width = sheetW + "px";
      sheet.style.height = pageH * scale + "px";
      if (String(page.border || "none").toLowerCase() === "thin")
        sheet.classList.add("has-page-border");

      const pageLabel = document.createElement("div");
      pageLabel.className = "designer-sheet-label";
      pageLabel.textContent = "หน้า " + (p + 1) + " / " + flow.pageCount;
      sheet.appendChild(pageLabel);

      const guideTop = flow.guideTop != null ? flow.guideTop : margins.top;
      const guideHeight = flow.guideHeight != null
        ? flow.guideHeight
        : Math.max(MIN_BLOCK_H, pageH - margins.top - margins.bottom);

      const guide = document.createElement("div");
      guide.className = "designer-margin-guide";
      guide.style.left = margins.left * scale + "px";
      guide.style.top = guideTop * scale + "px";
      guide.style.width = contentW * scale + "px";
      guide.style.height = guideHeight * scale + "px";
      sheet.appendChild(guide);

      // Inner band guides (inside margin box only — supers are outside)
      let bandY = 0;
      function addBandGuide(label, hMm, cls) {
        if (hMm <= 0) return;
        const g = document.createElement("div");
        g.className = "designer-band-guide " + (cls || "");
        g.style.left = margins.left * scale + "px";
        g.style.top = (guideTop + bandY) * scale + "px";
        g.style.width = contentW * scale + "px";
        g.style.height = Math.max(1, hMm * scale) + "px";
        g.textContent = label;
        sheet.appendChild(g);
        bandY += hMm;
      }
      if (flow.superHeaderH > 0) {
        const g = document.createElement("div");
        g.className = "designer-band-guide band-super";
        g.style.left = margins.left * scale + "px";
        g.style.top = (guideTop - flow.superHeaderH) * scale + "px";
        g.style.width = contentW * scale + "px";
        g.style.height = Math.max(1, flow.superHeaderH * scale) + "px";
        g.textContent = "super-header (นอก margin)";
        sheet.appendChild(g);
      }
      addBandGuide("header", flow.headerH || 0);
      addBandGuide("content", flow.contentFlowH || 0, "band-content band-guide-outline-only");
      addBandGuide("footer", flow.footerH || 0);
      if (flow.superFooterH > 0) {
        const g = document.createElement("div");
        g.className = "designer-band-guide band-super";
        g.style.left = margins.left * scale + "px";
        g.style.top = (guideTop + guideHeight) * scale + "px";
        g.style.width = contentW * scale + "px";
        g.style.height = Math.max(1, flow.superFooterH * scale) + "px";
        g.textContent = "super-footer (นอก margin)";
        sheet.appendChild(g);
      }

      if (p === 0) {
        const dropHint = document.createElement("div");
        dropHint.className = "designer-drop-hint hidden";
        dropHint.id = "designerDropHint";
        sheet.appendChild(dropHint);
      }

      sheet.addEventListener("click", () => {
        if (suppressClick) return;
        clearElementSelection();
        stateRef.selectedKey = null;
        renderInspector();
        document.querySelectorAll(".designer-element.selected").forEach((n) => n.classList.remove("selected"));
      });

      (flow.pages[p] || []).forEach((item, index) => {
        if (item.kind === "group-frame") {
          const frame = document.createElement("div");
          frame.className = "designer-group-frame";
          frame.style.left = item.xMm * scale + "px";
          frame.style.top = item.yMm * scale + "px";
          frame.style.width = item.wMm * scale + "px";
          frame.style.height = item.hMm * scale + "px";
          frame.dataset.groupId = item.el.id;
          frame.title = "Column stack · " + (item.el.children || []).length + " items";
          sheet.appendChild(frame);
          return;
        }
        if (item.kind === "stack-split") {
          const split = document.createElement("div");
          split.className = "stack-split";
          split.style.left = item.xMm * scale + "px";
          split.style.top = (item.yMm * scale - 4) + "px";
          split.style.width = item.wMm * scale + "px";
          split.title = "ลากเพื่อปรับความสูงชิ้นบน/ล่าง";
          split.dataset.groupId = item.groupId;
          split.dataset.splitIndex = String(item.index);
          split.addEventListener("pointerdown", (e) => {
            e.preventDefault();
            e.stopPropagation();
            startStackSplitDrag(e, item.groupId, item.index, m);
          });
          sheet.appendChild(split);
          return;
        }

        const el = item.el;
        if (!el) return;
        const wrap = document.createElement("div");
        wrap.className = "designer-element" + (isElementSelected(el.id) ? " selected" : "");
        if (item.outsideMargin || resolveBand(el) === "super-header" || resolveBand(el) === "super-footer") {
          wrap.classList.add("outside-margin");
        }
        if (!borderOn(el.chrome)) wrap.classList.add("no-border");
        // Page-absolute coordinates
        wrap.style.left = item.xMm * scale + "px";
        wrap.style.top = item.yMm * scale + "px";
        wrap.style.width = item.wMm * scale + "px";
        wrap.style.height = item.hMm * scale + "px";
        wrap.dataset.elementId = el.id;
        wrap.dataset.index = String(index);
        wrap.dataset.band = resolveBand(el);
        if (item.groupId) wrap.dataset.groupId = item.groupId;

        wrap.addEventListener("click", (e) => {
          e.stopPropagation();
          // Selection handled in pointerdown (incl. Shift); avoid double-toggle.
        });

        wrap.addEventListener("pointerdown", (e) => {
          if (e.target.closest(".resize-handle") || e.target.closest(".col-resize") || e.target.closest(".el-toolbar") || e.target.closest(".stack-split"))
            return;
          if (e.button !== 0) return;
          e.preventDefault();
          e.stopPropagation();
          stateRef.selectedKey = null;
          if (e.shiftKey) {
            toggleAdditiveSelection(el.id);
            renderAll();
            return;
          }
          // Keep multi-selection if dragging one of the already-selected group.
          if (!(selectedElementIds.size > 1 && selectedElementIds.has(el.id))) {
            setSingleSelection(el.id);
            document.querySelectorAll(".designer-element").forEach((n) => {
              n.classList.toggle("selected", n.dataset.elementId === el.id);
            });
          } else {
            selectedElementId = el.id;
          }
          // Sync inspector immediately — startMoveDrag only re-renders after a real drag.
          renderInspector();
          startMoveDrag(e, el, wrap, sheet, m);
        });

        const toolbar = document.createElement("div");
        toolbar.className = "el-toolbar";
        toolbar.innerHTML =
          `<span class="el-tag">${escapeHtml(el.type)} · ${escapeHtml(resolveBand(el))}${item.groupId ? " · stack" : ""}</span>`;
        const delBtn = document.createElement("button");
        delBtn.type = "button";
        delBtn.className = "el-del";
        delBtn.title = "ลบ widget";
        delBtn.textContent = "×";
        delBtn.addEventListener("pointerdown", (e) => e.stopPropagation());
        delBtn.addEventListener("click", (e) => {
          e.stopPropagation();
          deleteElement(el.id);
        });
        toolbar.appendChild(delBtn);
        wrap.appendChild(toolbar);

        const body = document.createElement("div");
        body.className = "el-body";
        const collapsePx = borderCollapseMm(scale) * scale;
        if (gaps.below <= 0 && (Number(el.box.yMm) || 0) > 0.01) {
          body.style.marginTop = collapsePx.toFixed(2) + "px";
          body.style.height = "calc(100% - " + collapsePx.toFixed(2) + "px)";
        }
        if (gaps.beside <= 0 && String(el.place || "below").toLowerCase() === "beside") {
          body.style.marginLeft = collapsePx.toFixed(2) + "px";
          body.style.width = "calc(100% - " + collapsePx.toFixed(2) + "px)";
        }
        if (el.type === "config-table") {
          const catalogOrInline = resolveTablePreset(el);
          if (catalogOrInline) {
            const rowMode = String((catalogOrInline.rowMode
              || (el.tablePreset && el.tablePreset.rowMode)
              || "")).toLowerCase();
            if (rowMode === "matrix") {
              body.appendChild(renderChecklistGridHtml(el, sampleData, scale));
            } else if (global.TableLayoutEngine) {
              const preset = ensureWorkingPreset(el, catalogOrInline);
              const model = global.TableLayoutEngine.buildLayout(preset, el, labels, sampleData, item.hMm);
              body.appendChild(renderTableHtml(model, el, scale, item.hMm));
            } else {
              body.innerHTML = `<div class="ph-dense">config-table</div>`;
            }
          } else {
            body.innerHTML = `<div class="ph-dense">config-table</div>`;
          }
        } else if (el.type === "box-text") {
          body.appendChild(renderBoxTextHtml(el, sampleData, scale));
        } else if (el.type === "page-of") {
          body.appendChild(renderPageOfHtml(el, p + 1, flow.pageCount, scale));
        } else if (el.type === "header") {
          const catalogOrInline = resolveHeaderPreset(el);
          if (catalogOrInline && global.HeaderLayoutEngine) {
            const preset = ensureWorkingHeaderPreset(el, catalogOrInline);
            const titleFallback = (sampleData && sampleData.title) || "Header";
            const model = global.HeaderLayoutEngine.buildLayout(
              preset,
              sampleData,
              titleFallback,
              el.bottomMode);
            body.appendChild(renderHeaderHtml(model, el, scale));
          } else {
            const patient = sampleData && sampleData.header && sampleData.header.patient;
            const title = (sampleData && sampleData.title) || "Header";
            body.classList.add("designer-header-placeholder");
            body.innerHTML =
              `<div class="ph-title">${escapeHtml(title)}</div>` +
              `<div class="ph-meta">${escapeHtml((patient && patient.name) || "Patient")} · HN ${escapeHtml((patient && patient.hn) || "—")}</div>`;
          }
        } else if (el.type === "dense") {
          body.appendChild(renderDenseWidgetHtml(el, sampleData, scale, item.hMm, labels));
        } else if (el.type === "data-grid") {
          body.appendChild(renderDataGridHtml(el, sampleData, scale, item.hMm));
        } else if (el.type === "narrative") {
          body.appendChild(renderNarrativeHtml(el, sampleData, scale));
        } else {
          body.innerHTML = `<div class="ph-dense">${escapeHtml(el.type)}: ${escapeHtml(el.widget || el.id)}</div>`;
        }
        wrap.appendChild(body);

        ["e", "s", "se"].forEach((dir) => {
          const h = document.createElement("div");
          h.className = "resize-handle rh-" + dir;
          h.dataset.dir = dir;
          h.addEventListener("pointerdown", (e) => {
            e.preventDefault();
            e.stopPropagation();
            startResizeDrag(e, el, dir, m);
          });
          wrap.appendChild(h);
        });

        sheet.appendChild(wrap);
      });

      stack.appendChild(sheet);
    }

    host.appendChild(stack);
  }

  /** Mirrors DataGridRows.IsSectionBand — known frequency titles only (1/3/6/12 month, Other). */
  const DATA_GRID_SECTION_TITLES = {
    "1 month": true,
    "3 month": true,
    "6 month": true,
    "1 year": true,
    other: true,
  };

  function isDataGridSectionBand(row) {
    if (!row || row.length < 2) return false;
    const title = String(row[0] || "").trim();
    if (!title || !DATA_GRID_SECTION_TITLES[title.toLowerCase()]) return false;
    for (let i = 1; i < row.length; i++) {
      if (row[i] != null && String(row[i]).trim() !== "") return false;
    }
    return true;
  }

  function renderDataGridHtml(el, data, scale, boxHeightMm) {
    const root = document.createElement("div");
    root.className = "cfg-table" + (borderOn(el.chrome) ? "" : " cfg-no-border");
    const fill = (el.chrome && el.chrome.headerFill) || "#E8EEF5";
    const fs = (el.chrome && el.chrome.fontSize) || 7;
    root.style.fontSize = (fs * (scale / 2.5)).toFixed(1) + "px";
    root.style.height = "100%";

    const model = resolveDataGridModel(el, data);
    const headers = model.headers.length ? model.headers : [""];
    const rows = model.rows.length ? model.rows : [headers.map(() => "")];
    ensureDataGridColumnWidths(el, headers.length);
    const weights = resolveDataGridColumnWeights(el.chrome && el.chrome.columnWidths, headers.length);
    const sum = weights.reduce((a, b) => a + b, 0) || 1;
    const bodyRowCount = rows.length;
    const boxPx = Math.max(0, Number(boxHeightMm) || 0) * scale;
    let headerPx = Math.max(4, (boxPx / (bodyRowCount + 1)) || 4);
    let slotPx = Math.max(3, headerPx);
    if (boxPx > 0) {
      headerPx = Math.max(4, boxPx / (bodyRowCount + 1));
      slotPx = headerPx;
    }

    const table = document.createElement("table");
    table.className = "cfg-table-grid";
    table.style.height = boxPx > 0 ? boxPx.toFixed(2) + "px" : "100%";

    const colgroup = document.createElement("colgroup");
    weights.forEach((w) => {
      const col = document.createElement("col");
      col.style.width = ((w / sum) * 100).toFixed(3) + "%";
      colgroup.appendChild(col);
    });
    table.appendChild(colgroup);

    const thead = document.createElement("thead");
    thead.className = "dg-date-head";
    const hr = document.createElement("tr");
    hr.style.height = headerPx.toFixed(2) + "px";
    headers.forEach((h) => {
      const th = document.createElement("th");
      th.textContent = String(h || "").trim() ? String(h) : "\u00A0";
      th.style.height = headerPx.toFixed(2) + "px";
      hr.appendChild(th);
    });
    thead.appendChild(hr);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");
    const bandFill = String(fill).indexOf("$") !== 0 ? fill : null;
    rows.forEach((row) => {
      const tr = document.createElement("tr");
      tr.style.height = slotPx.toFixed(2) + "px";
      if (isDataGridSectionBand(row)) {
        tr.className = "dg-section-band";
        const td = document.createElement("td");
        td.colSpan = headers.length;
        td.textContent = String(row[0]).trim();
        if (bandFill) td.style.background = bandFill;
        tr.appendChild(td);
      } else {
        headers.forEach((_, i) => {
          const td = document.createElement("td");
          td.textContent = (row[i] != null && String(row[i]).trim() !== "") ? String(row[i]) : "\u00A0";
          tr.appendChild(td);
        });
      }
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    root.appendChild(table);
    attachDataGridColumnResizers(root, table, el, weights.slice());
    return root;
  }

  function attachDataGridColumnResizers(root, table, el, headerWeights) {
    requestAnimationFrame(() => {
      const ths = table.querySelectorAll("thead th");
      if (ths.length < 2) return;
      const overlay = document.createElement("div");
      overlay.className = "col-resize-layer";
      root.appendChild(overlay);
      const rootRect = root.getBoundingClientRect();
      ths.forEach((th, hi) => {
        if (hi >= ths.length - 1) return;
        const rect = th.getBoundingClientRect();
        const handle = document.createElement("div");
        handle.className = "col-resize";
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
        handle.title = "ลากปรับความกว้างคอลัมน์ data-grid (ส่งผลต่อ PDF)";
        handle.addEventListener("pointerdown", (e) => {
          e.preventDefault();
          e.stopPropagation();
          startDataGridColumnResize(e, el, hi, headerWeights.slice(), table, handle, root);
        });
        overlay.appendChild(handle);
      });
    });
  }

  function startDataGridColumnResize(e, el, headerIndex, headerWeights, table, handle, root) {
    if (canvasTools) canvasTools.pushHistory();
    const startX = e.clientX;
    const leftW = headerWeights[headerIndex];
    const rightW = headerWeights[headerIndex + 1];
    const pair = leftW + rightW;
    const metrics = pageMetrics();

    function applyColgroup() {
      const sum = headerWeights.reduce((a, b) => a + b, 0) || 1;
      const cols = table.querySelectorAll("colgroup col");
      headerWeights.forEach((w, i) => {
        if (cols[i]) cols[i].style.width = ((w / sum) * 100).toFixed(3) + "%";
      });
    }

    function onMove(ev) {
      const dxMm = (ev.clientX - startX) / metrics.scale;
      const total = headerWeights.reduce((a, b) => a + b, 0);
      const dWeight = (dxMm / Math.max(1, el.box.wMm)) * total;
      let newLeft = Math.max(MIN_COL_WEIGHT, leftW + dWeight);
      let newRight = Math.max(MIN_COL_WEIGHT, pair - newLeft);
      headerWeights[headerIndex] = newLeft;
      headerWeights[headerIndex + 1] = newRight;
      writeDataGridWeightsToChrome(el, headerWeights);
      applyColgroup();
      const ths = table.querySelectorAll("thead th");
      const th = ths[headerIndex];
      if (th && handle && root) {
        const rootRect = root.getBoundingClientRect();
        const rect = th.getBoundingClientRect();
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
      }
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
      if (setStatusRef) setStatusRef("data-grid columnWidths อัปเดต — Download PDF จะใช้ชุดนี้", "ok");
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  /**
   * HTML table that mirrors ConfigurableTableComposer (QuestPDF):
   * - Header: DATE (month+day width) | data columns
   * - Body: month rowspan | day | data columns
   * - Row heights locked to layout engine mm (parity with Download PDF)
   */
  function renderTableHtml(model, el, scale, boxHeightMm) {
    const root = document.createElement("div");
    root.className = "cfg-table" + (borderOn(el.chrome) ? "" : " cfg-no-border");
    const p = model.preset;
    const isGrouped = String(p.rowMode || "").toLowerCase() !== "freedom";
    const monthW = Number((p.dateColumns && p.dateColumns.monthWeight) || 0.45);
    const dayW = Number((p.dateColumns && p.dateColumns.dayWeight) || 0);
    const dayWSafe = dayW > 0 ? dayW : 1.35;
    const cols = p.columns || [];
    const colWeights = cols.map((c) => Math.max(0.1, Number(c.weight) || 1));

    // Visual header weights match PDF RelativeItem list: [month+day, ...cols]
    const headerWeights = isGrouped
      ? [monthW + dayWSafe].concat(colWeights)
      : colWeights.slice();
    const sum = headerWeights.reduce((a, b) => a + b, 0) || 1;

    const bodyRowCount = Math.max(1, (model.rows && model.rows.length) || 1);
    const boxPx = Math.max(0, Number(boxHeightMm) || 0) * scale;
    let headerPx = Math.max(4, model.headerHeightMm * scale);
    let slotPx = Math.max(3, model.slotHeightMm * scale);
    let tablePx = headerPx + slotPx * bodyRowCount;
    // Stretch rows to fill box so bottom border meets the block edge (no corner gap).
    if (boxPx > tablePx + 0.5) {
      tablePx = boxPx;
      slotPx = Math.max(3, (boxPx - headerPx) / bodyRowCount);
    }
    const nbsp = "\u00A0";

    const table = document.createElement("table");
    table.className = "cfg-table-grid";
    table.style.height = tablePx.toFixed(2) + "px";
    table.style.maxHeight = tablePx.toFixed(2) + "px";
    root.style.height = "100%";

    const colgroup = document.createElement("colgroup");
    if (isGrouped) {
      const bodySum = monthW + dayWSafe + colWeights.reduce((a, b) => a + b, 0);
      [monthW, dayWSafe].concat(colWeights).forEach((w) => {
        const col = document.createElement("col");
        col.style.width = ((w / (bodySum || 1)) * 100).toFixed(3) + "%";
        colgroup.appendChild(col);
      });
    } else {
      colWeights.forEach((w) => {
        const col = document.createElement("col");
        col.style.width = ((w / sum) * 100).toFixed(3) + "%";
        colgroup.appendChild(col);
      });
    }
    table.appendChild(colgroup);

    const thead = document.createElement("thead");
    const hr = document.createElement("tr");
    hr.style.height = headerPx.toFixed(2) + "px";
    if (isGrouped) {
      const thDate = document.createElement("th");
      thDate.colSpan = 2;
      thDate.textContent = model.headerLabels[0] || "วัน/เดือน/ปี";
      thDate.style.height = headerPx.toFixed(2) + "px";
      hr.appendChild(thDate);
      cols.forEach((c, i) => {
        const th = document.createElement("th");
        th.textContent = model.headerLabels[i + 1] || c.title || c.id;
        th.style.height = headerPx.toFixed(2) + "px";
        hr.appendChild(th);
      });
    } else {
      cols.forEach((c, i) => {
        const th = document.createElement("th");
        th.textContent = model.headerLabels[i] || c.title || c.id;
        th.style.height = headerPx.toFixed(2) + "px";
        hr.appendChild(th);
      });
    }
    thead.appendChild(hr);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");
    if (!isGrouped) {
      model.rows.forEach((row, rowIndex) => {
        const tr = document.createElement("tr");
        tr.style.height = slotPx.toFixed(2) + "px";
        cols.forEach((colDef, ci) => {
          const cell = (row.cells && row.cells[ci]) || { text: " " };
          const td = document.createElement("td");
          const kind = String((colDef && colDef.cellKind) || "text").toLowerCase();
          if (kind === "soap-progress") {
            td.className = "soap-progress-cell";
            td.appendChild(buildSoapProgressSchematic(
              sampleData,
              row.slotIndex != null ? row.slotIndex : rowIndex,
              slotPx,
              el));
          } else {
            td.textContent = cellText(cell.text, nbsp);
            if (cell.historical) td.className = "historical";
            if (cell.center || (colDef && colDef.center)) td.classList.add("center");
          }
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
    } else {
      let g = -1;
      model.rows.forEach((row) => {
        const tr = document.createElement("tr");
        tr.style.height = slotPx.toFixed(2) + "px";
        if (row.groupIndex !== g) {
          g = row.groupIndex;
          if (row.slotIndex === 0) {
            const tdMonth = document.createElement("td");
            tdMonth.rowSpan = Math.max(1, p.slotsPerGroup);
            tdMonth.className = "month-cell";
            tdMonth.textContent = cellText(row.groupLabel, nbsp);
            tr.appendChild(tdMonth);
          }
        }
        row.cells.forEach((cell, ci) => {
          const td = document.createElement("td");
          td.textContent = cellText(cell.text, nbsp);
          if (cell.historical) td.className = "historical";
          if (cell.center || ci === 0) td.classList.add("center");
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
    }
    table.appendChild(tbody);
    root.appendChild(table);

    attachColumnResizers(root, table, el, p, headerWeights, isGrouped, monthW, dayWSafe);
    if (cols.some((c) => String((c && c.cellKind) || "").toLowerCase() === "soap-progress")) {
      attachSoapBandSplitters(root, el);
    }
    return root;
  }

  function mergeTableChrome(el) {
    const preset = resolveTablePreset(el) || {};
    return Object.assign({}, preset.chrome || {}, (el && el.chrome) || {});
  }

  function buildSoapProgressSchematic(data, sessionIndex, slotPx, el) {
    const wrap = document.createElement("div");
    wrap.className = "soap-progress-schematic";
    wrap.style.height = "100%";
    const chrome = mergeTableChrome(el);
    const bands = soapBandWeights(chrome);
    const bandSum = bands.reduce((a, b) => a + b, 0) || 1;
    const sessions = (data && Array.isArray(data.sessions) ? data.sessions : []);
    const session = sessions[sessionIndex] || null;
    const bandDefs = [
      { letter: "S", text: session && session.subjective },
      { letter: "O", text: formatSoapObjective(session) },
      { letter: "A", text: session && session.assessment },
      { letter: "P", text: session && session.plan },
    ];
    wrap.style.display = "grid";
    wrap.style.gridTemplateRows = bands.map((b) => (b / bandSum).toFixed(4) + "fr").join(" ");
    bandDefs.forEach((b, bi) => {
      const band = document.createElement("div");
      band.className = "dense-soap-band";
      band.dataset.bandIndex = String(bi);
      band.innerHTML = `<span class="dense-soap-letter">${escapeHtml(b.letter)}</span>` +
        `<span class="dense-soap-text">${escapeHtml(b.text || "")}</span>`;
      wrap.appendChild(band);
    });
    void slotPx;
    return wrap;
  }

  /** Drag horizontal splitters inside soap-progress cells to edit chrome.bandWeights. */
  function attachSoapBandSplitters(root, el) {
    requestAnimationFrame(() => {
      const firstSchematic = root.querySelector(".soap-progress-schematic");
      if (!firstSchematic) return;
      const bands = firstSchematic.querySelectorAll(".dense-soap-band");
      if (bands.length < 2) return;
      const overlay = document.createElement("div");
      overlay.className = "soap-band-resize-layer";
      root.appendChild(overlay);
      const rootRect = root.getBoundingClientRect();
      const cellRect = firstSchematic.getBoundingClientRect();
      for (let i = 0; i < bands.length - 1; i++) {
        const rect = bands[i].getBoundingClientRect();
        const handle = document.createElement("div");
        handle.className = "soap-band-resize";
        handle.style.left = (cellRect.left - rootRect.left) + "px";
        handle.style.width = cellRect.width + "px";
        handle.style.top = (rect.bottom - rootRect.top - 3) + "px";
        handle.title = "ลากปรับสัดส่วนแถบ S/O/A/P (bandWeights)";
        handle.dataset.splitIndex = String(i);
        handle.addEventListener("pointerdown", (e) => {
          e.preventDefault();
          e.stopPropagation();
          startSoapBandResize(e, el, i, root);
        });
        overlay.appendChild(handle);
      }
    });
  }

  function startSoapBandResize(e, el, splitIndex, root) {
    if (canvasTools) canvasTools.pushHistory();
    el.chrome = mergeTableChrome(el);
    const weights = soapBandWeights(el.chrome).slice();
    const startY = e.clientY;
    const above = weights[splitIndex];
    const below = weights[splitIndex + 1];
    const pair = above + below;
    const cell = root.querySelector(".soap-progress-schematic");
    const cellH = cell ? cell.getBoundingClientRect().height : 100;

    function onMove(ev) {
      const dy = ev.clientY - startY;
      const dW = (dy / Math.max(1, cellH)) * pair;
      const newAbove = Math.max(0.2, above + dW);
      const newBelow = Math.max(0.2, pair - newAbove);
      weights[splitIndex] = newAbove;
      weights[splitIndex + 1] = newBelow;
      el.chrome.bandWeights = weights.map((w) => Math.round(w * 100) / 100);
      const working = el.tablePreset;
      if (working) {
        working.chrome = working.chrome || {};
        working.chrome.bandWeights = el.chrome.bandWeights.slice();
      }
      // Live update grid rows on all schematics
      const sum = weights.reduce((a, b) => a + b, 0) || 1;
      root.querySelectorAll(".soap-progress-schematic").forEach((sch) => {
        sch.style.gridTemplateRows = weights.map((b) => (b / sum).toFixed(4) + "fr").join(" ");
      });
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      renderAll();
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  function cellText(text, nbsp) {
    if (text == null || text === "" || text === " ") return nbsp;
    return String(text);
  }

  function applyColgroupWidths(table, working, isGrouped) {
    const colgroup = table.querySelector("colgroup");
    if (!colgroup) return;
    const monthW = Number((working.dateColumns && working.dateColumns.monthWeight) || 0.45);
    const dayW = Number((working.dateColumns && working.dateColumns.dayWeight) || 1.35);
    const colWeights = (working.columns || []).map((c) => Math.max(0.1, Number(c.weight) || 1));
    const widths = isGrouped
      ? [monthW, dayW].concat(colWeights)
      : colWeights.slice();
    const total = widths.reduce((a, b) => a + b, 0) || 1;
    const cols = colgroup.querySelectorAll("col");
    widths.forEach((w, i) => {
      if (cols[i]) cols[i].style.width = ((w / total) * 100).toFixed(3) + "%";
    });
  }

  function attachColumnResizers(root, table, el, preset, headerWeights, isGrouped, monthW, dayW) {
    const working = ensureWorkingPreset(el, preset);

    requestAnimationFrame(() => {
      const ths = table.querySelectorAll("thead th");
      if (!ths.length) return;
      const overlay = document.createElement("div");
      overlay.className = "col-resize-layer";
      root.appendChild(overlay);
      const rootRect = root.getBoundingClientRect();
      ths.forEach((th, hi) => {
        if (hi >= ths.length - 1) return;
        const rect = th.getBoundingClientRect();
        const handle = document.createElement("div");
        handle.className = "col-resize";
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
        handle.title = "ลากปรับความกว้างคอลัมน์ (ส่งผลต่อ PDF)";
        handle.addEventListener("pointerdown", (e) => {
          e.preventDefault();
          e.stopPropagation();
          startColumnResize(e, el, working, hi, headerWeights.slice(), isGrouped, monthW, dayW, table, handle, root);
        });
        overlay.appendChild(handle);
      });
    });
  }

  function startColumnResize(e, el, working, headerIndex, headerWeights, isGrouped, monthW, dayW, table, handle, root) {
    if (canvasTools) canvasTools.pushHistory();
    const startX = e.clientX;
    const leftW = headerWeights[headerIndex];
    const rightW = headerWeights[headerIndex + 1];
    const pair = leftW + rightW;
    const metrics = pageMetrics();
    const dateRatioMonth = (monthW + dayW) > 0 ? monthW / (monthW + dayW) : 0.25;

    function onMove(ev) {
      const dxMm = (ev.clientX - startX) / metrics.scale;
      const total = headerWeights.reduce((a, b) => a + b, 0);
      const dWeight = (dxMm / Math.max(1, el.box.wMm)) * total;
      let newLeft = Math.max(MIN_COL_WEIGHT, leftW + dWeight);
      let newRight = Math.max(MIN_COL_WEIGHT, pair - newLeft);
      headerWeights[headerIndex] = newLeft;
      headerWeights[headerIndex + 1] = newRight;
      applyHeaderWeightsToPreset(working, headerWeights, isGrouped, dateRatioMonth);
      commitWorking(el, working);
      applyColgroupWidths(table, working, isGrouped);
      // Move handle with divider (approx from header cell)
      const ths = table.querySelectorAll("thead th");
      const th = ths[headerIndex];
      if (th && handle && root) {
        const rootRect = root.getBoundingClientRect();
        const rect = th.getBoundingClientRect();
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
      }
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
      if (setStatusRef) setStatusRef("คอลัมน์อัปเดต — Download PDF จะใช้ความกว้างชุดนี้", "ok");
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  /** headerWeights: grouped = [month+day, ...cols]; freedom = [...cols] */
  function applyHeaderWeightsToPreset(working, headerWeights, isGrouped, dateRatioMonth) {
    if (isGrouped) {
      const dateTotal = headerWeights[0];
      working.dateColumns = working.dateColumns || {};
      working.dateColumns.monthWeight = Math.max(MIN_COL_WEIGHT, dateTotal * dateRatioMonth);
      working.dateColumns.dayWeight = Math.max(MIN_COL_WEIGHT, dateTotal * (1 - dateRatioMonth));
      (working.columns || []).forEach((c, i) => {
        c.weight = headerWeights[i + 1] || c.weight || 1;
      });
    } else {
      (working.columns || []).forEach((c, i) => {
        c.weight = headerWeights[i] || c.weight || 1;
      });
    }
  }

  function applyWeightsToPreset(working, weights, isGrouped, dataOffset) {
    // kept for compatibility; prefer applyHeaderWeightsToPreset
    if (isGrouped) {
      working.dateColumns = working.dateColumns || {};
      working.dateColumns.monthWeight = weights[0];
      working.dateColumns.dayWeight = weights[1];
    }
    (working.columns || []).forEach((c, i) => {
      c.weight = weights[dataOffset + i] || c.weight || 1;
    });
  }

  function nbspOr(text) {
    if (text == null || text === "") return "\u00A0";
    return String(text);
  }

  function renderDenseWidgetHtml(el, data, scale, boxHeightMm, packLabels) {
    const widget = String(el.widget || "").toLowerCase();
    if (widget === "clinical.soap-table")
      return renderSoapTableHtml(el, data, scale, boxHeightMm, packLabels);
    if (widget === "clinical.checklist-patient")
      return renderChecklistPatientHtml(data, scale);
    if (widget === "clinical.checklist-grid")
      return renderChecklistGridHtml(el, data, scale);
    if (widget === "clinical.checklist-text-notes")
      return renderChecklistTextNotesHtml(data, scale);
    if (widget === "clinical.consent-narrative")
      return renderConsentDenseHtml(el, data, scale);

    const root = document.createElement("div");
    root.className = "ph-dense";
    root.textContent = "dense: " + (el.widget || el.id);
    return root;
  }

  function renderConsentDenseHtml(el, data, scale) {
    const mode = String((el.chrome && el.chrome.contentMode) || "full").toLowerCase();
    const root = document.createElement("div");
    root.className = "narrative-block consent-dense-mock";
    root.style.fontSize = Math.max(8, 10 * (scale / 2.5)).toFixed(1) + "px";
    const title = (data && data.title) || "Consent";
    if (mode === "closing") {
      root.innerHTML =
        `<div class="narrative-p muted">ลายเซ็น / signatures (C#)</div>` +
        `<div class="narrative-p muted">validity note</div>`;
      return root;
    }
    if (mode === "intro") {
      root.innerHTML =
        `<div class="narrative-p role-title">${escapeHtml(title)}</div>` +
        `<div class="narrative-p">ข้าพเจ้า / I am … (fill-in C#)</div>` +
        `<div class="narrative-p muted">intro · contentMode=intro</div>`;
      return root;
    }
    root.innerHTML =
      `<div class="narrative-p role-title">${escapeHtml(title)}</div>` +
      `<div class="narrative-p">intro + body + signatures (full)</div>` +
      `<div class="narrative-p muted">ใช้ narrative element สำหรับย่อหน้า body</div>`;
    return root;
  }

  /** Studio prefers pack paragraphs so Word-lite edits show; PDF may still bind. */
  function resolveNarrativeParagraphs(el, data) {
    if (Array.isArray(el.paragraphs) && el.paragraphs.length)
      return el.paragraphs;
    if (el.bindParagraphs && data && global.HeaderLayoutEngine) {
      const v = global.HeaderLayoutEngine.readAt(data, el.bindParagraphs);
      if (Array.isArray(v) && v.length) {
        return v.map((item) => {
          if (typeof item === "string") return { text: item, sub: false };
          return {
            text: (item && item.text) || "",
            sub: !!(item && item.sub),
            align: (item && item.align) || undefined,
            role: (item && item.role) || undefined,
          };
        }).filter((p) => p.text && String(p.text).trim());
      }
    }
    return [];
  }

  function renderNarrativeHtml(el, data, scale) {
    const root = document.createElement("div");
    root.className = "narrative-block" + (borderOn(el.chrome) ? "" : " narrative-no-border");
    const fs = (el.chrome && el.chrome.fontSize) || 11;
    root.style.fontSize = Math.max(8, fs * (scale / 2.5)).toFixed(1) + "px";
    const paras = resolveNarrativeParagraphs(el, data);
    if (!paras.length) {
      root.innerHTML = `<div class="narrative-p muted">(empty narrative — แก้ใน Inspector)</div>`;
      return root;
    }
    paras.forEach((p) => {
      const div = document.createElement("div");
      const role = String(p.role || "body").toLowerCase();
      div.className = "narrative-p" + (p.sub ? " sub" : "") + (role === "title" ? " role-title" : "") + (role === "note" ? " role-note" : "");
      const align = String(p.align || (role === "title" ? "center" : "left")).toLowerCase();
      div.style.textAlign = align === "right" || align === "center" ? align : "left";
      div.textContent = p.text || "\u00A0";
      root.appendChild(div);
    });
    return root;
  }

  function renderNarrativeInspector(insp, el) {
    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent =
      "Narrative (Word-lite) — แก้ย่อหน้า / จัดบรรทัด / เยื้อง sub / จัดชิด. Soft break ใช้ Enter ในช่องข้อความ.";
    insp.appendChild(tip);

    const bindLab = document.createElement("label");
    bindLab.textContent = "bindParagraphs (optional, PDF override body)";
    const bindIn = document.createElement("input");
    bindIn.type = "text";
    bindIn.value = el.bindParagraphs || "";
    bindIn.placeholder = "$.bodyParagraphs";
    bindIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.bindParagraphs = bindIn.value.trim() || undefined;
      renderAll();
    });
    bindLab.appendChild(bindIn);
    insp.appendChild(bindLab);

    const fsLab = document.createElement("label");
    fsLab.textContent = "fontSize";
    const fsIn = document.createElement("input");
    fsIn.type = "number";
    fsIn.step = "0.5";
    fsIn.value = String((el.chrome && el.chrome.fontSize) || 11);
    fsIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.chrome = el.chrome || {};
      el.chrome.fontSize = Number(fsIn.value) || 11;
      renderAll();
    });
    fsLab.appendChild(fsIn);
    insp.appendChild(fsLab);

    const spaceLab = document.createElement("label");
    spaceLab.textContent = "paragraph spacing (rowHeightMm)";
    const spaceIn = document.createElement("input");
    spaceIn.type = "number";
    spaceIn.step = "0.5";
    spaceIn.value = String((el.chrome && el.chrome.rowHeightMm) || 3.5);
    spaceIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.chrome = el.chrome || {};
      el.chrome.rowHeightMm = Number(spaceIn.value) || 3.5;
      renderAll();
    });
    spaceLab.appendChild(spaceIn);
    insp.appendChild(spaceLab);

    const head = document.createElement("p");
    head.innerHTML = "<strong>Paragraphs</strong>";
    insp.appendChild(head);

    if (!Array.isArray(el.paragraphs)) el.paragraphs = [];

    el.paragraphs.forEach((para, idx) => {
      const box = document.createElement("div");
      box.className = "box-text-item-edit narrative-para-edit";

      const ta = document.createElement("textarea");
      ta.rows = 3;
      ta.value = para.text || "";
      ta.placeholder = "ข้อความย่อหน้า…";
      ta.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        para.text = ta.value;
        renderAll();
      });
      box.appendChild(ta);

      const row = document.createElement("div");
      row.className = "col-row";

      const roleSel = document.createElement("select");
      roleSel.title = "role";
      ["body", "title", "note"].forEach((r) => {
        const o = document.createElement("option");
        o.value = r;
        o.textContent = r;
        if (String(para.role || "body") === r) o.selected = true;
        roleSel.appendChild(o);
      });
      roleSel.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        para.role = roleSel.value === "body" ? undefined : roleSel.value;
        renderAll();
      });
      row.appendChild(roleSel);

      const alignSel = document.createElement("select");
      alignSel.title = "align";
      ["left", "center", "right"].forEach((a) => {
        const o = document.createElement("option");
        o.value = a;
        o.textContent = a;
        if (String(para.align || "left") === a) o.selected = true;
        alignSel.appendChild(o);
      });
      alignSel.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        para.align = alignSel.value;
        renderAll();
      });
      row.appendChild(alignSel);

      const subLab = document.createElement("label");
      subLab.className = "inline-check";
      const subCb = document.createElement("input");
      subCb.type = "checkbox";
      subCb.checked = !!para.sub;
      subCb.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        para.sub = subCb.checked;
        renderAll();
      });
      subLab.appendChild(subCb);
      subLab.appendChild(document.createTextNode(" sub"));
      row.appendChild(subLab);

      const up = document.createElement("button");
      up.type = "button";
      up.textContent = "↑";
      up.title = "Move up";
      up.disabled = idx === 0;
      up.addEventListener("click", () => {
        if (idx === 0) return;
        if (canvasTools) canvasTools.pushHistory();
        const tmp = el.paragraphs[idx - 1];
        el.paragraphs[idx - 1] = el.paragraphs[idx];
        el.paragraphs[idx] = tmp;
        renderAll();
      });
      row.appendChild(up);

      const down = document.createElement("button");
      down.type = "button";
      down.textContent = "↓";
      down.title = "Move down";
      down.disabled = idx >= el.paragraphs.length - 1;
      down.addEventListener("click", () => {
        if (idx >= el.paragraphs.length - 1) return;
        if (canvasTools) canvasTools.pushHistory();
        const tmp = el.paragraphs[idx + 1];
        el.paragraphs[idx + 1] = el.paragraphs[idx];
        el.paragraphs[idx] = tmp;
        renderAll();
      });
      row.appendChild(down);

      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "✕";
      del.title = "Remove";
      del.addEventListener("click", () => {
        if (canvasTools) canvasTools.pushHistory();
        el.paragraphs.splice(idx, 1);
        renderAll();
      });
      row.appendChild(del);

      box.appendChild(row);
      insp.appendChild(box);
    });

    const add = document.createElement("button");
    add.type = "button";
    add.textContent = "+ ย่อหน้า";
    add.addEventListener("click", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.paragraphs.push({ text: "ย่อหน้าใหม่", sub: false, align: "left" });
      renderAll();
    });
    insp.appendChild(add);
  }

  function soapColWidths(chrome) {
    const raw = (chrome && chrome.columnWidths) || ["18mm", "2.4", "1.1", "1.1"];
    const parsed = raw.map((v) => {
      const s = String(v);
      if (s.endsWith("mm")) return { kind: "mm", value: parseFloat(s) || 18 };
      return { kind: "rel", value: parseFloat(s) || 1 };
    });
    while (parsed.length < 4) parsed.push({ kind: "rel", value: 1 });
    let fixed = 0;
    let weightSum = 0;
    parsed.forEach((p) => {
      if (p.kind === "mm") fixed += p.value;
      else weightSum += p.value;
    });
    const remain = Math.max(1, 100 - (fixed > 0 ? 0 : 0));
    // Use CSS grid: fixed mm columns as fr approximations via % of typical 206mm.
    const baseW = 206;
    return parsed.map((p) => {
      if (p.kind === "mm") return ((p.value / baseW) * 100).toFixed(2) + "%";
      const pct = ((100 - (fixed / baseW) * 100) * (p.value / (weightSum || 1)));
      return Math.max(4, pct).toFixed(2) + "%";
    });
  }

  function soapBandWeights(chrome) {
    const w = (chrome && chrome.bandWeights) || [1, 2.5, 1, 1];
    const nums = w.map((x) => Math.max(0.1, Number(x) || 1));
    while (nums.length < 4) nums.push(1);
    return nums.slice(0, 4);
  }

  function renderSoapTableHtml(el, data, scale, boxHeightMm, packLabels) {
    const root = document.createElement("div");
    root.className = "dense-soap";
    root.style.height = "100%";
    const chrome = el.chrome || {};
    const cols = soapColWidths(chrome);
    const bands = soapBandWeights(chrome);
    const bandSum = bands.reduce((a, b) => a + b, 0) || 1;
    const labelsMap = packLabels || {};
    const hDate = labelsMap.colDate || "DATE";
    const hProg = labelsMap.colProgress || "PROGRESS NOTE";
    const hOne = labelsMap.colOrderOneDay || "ORDER FOR ONE DAY";
    const hCont = labelsMap.colOrderContinuation || "ORDER FOR CONTINUATION";
    const headerH = Math.max(10, (Number(chrome.headerHeightMm) || 5) * scale);
    const sessions = (data && Array.isArray(data.sessions) ? data.sessions : []).slice();
    while (sessions.length < 2) sessions.push(null);
    const rowH = Math.max(40, ((Number(boxHeightMm) || 255) - (Number(chrome.headerHeightMm) || 5)) / sessions.length * scale);

    const table = document.createElement("div");
    table.className = "dense-soap-table";
    table.style.display = "grid";
    table.style.gridTemplateColumns = cols.join(" ");
    table.style.height = "100%";

    [hDate, hProg, hOne, hCont].forEach((text) => {
      const cell = document.createElement("div");
      cell.className = "dense-soap-th";
      cell.style.minHeight = headerH.toFixed(2) + "px";
      cell.textContent = text;
      table.appendChild(cell);
    });

    sessions.forEach((session) => {
      const date = document.createElement("div");
      date.className = "dense-soap-td dense-soap-date";
      date.style.minHeight = rowH.toFixed(2) + "px";
      date.textContent = (session && session.dateLabel) || "";
      table.appendChild(date);

      const soap = document.createElement("div");
      soap.className = "dense-soap-td dense-soap-bands";
      soap.style.minHeight = rowH.toFixed(2) + "px";
      soap.style.display = "grid";
      soap.style.gridTemplateRows = bands.map((b) => (b / bandSum).toFixed(4) + "fr").join(" ");
      const bandDefs = [
        { letter: "S", text: session && session.subjective },
        { letter: "O", text: formatSoapObjective(session) },
        { letter: "A", text: session && session.assessment },
        { letter: "P", text: session && session.plan },
      ];
      bandDefs.forEach((b) => {
        const band = document.createElement("div");
        band.className = "dense-soap-band";
        band.innerHTML = `<span class="dense-soap-letter">${escapeHtml(b.letter)}</span>` +
          `<span class="dense-soap-text">${escapeHtml(b.text || "")}</span>`;
        soap.appendChild(band);
      });
      table.appendChild(soap);

      const one = document.createElement("div");
      one.className = "dense-soap-td";
      one.style.minHeight = rowH.toFixed(2) + "px";
      one.textContent = (session && session.orderForOneDay) || "";
      table.appendChild(one);

      const cont = document.createElement("div");
      cont.className = "dense-soap-td";
      cont.style.minHeight = rowH.toFixed(2) + "px";
      cont.textContent = (session && session.orderForContinuation) || "";
      table.appendChild(cont);
    });

    root.appendChild(table);
    return root;
  }

  function formatSoapObjective(session) {
    if (!session) return "";
    const bits = [];
    if (session.generalAppearance) bits.push(String(session.generalAppearance));
    if (session.heent) bits.push("HEENT:" + session.heent);
    if (session.lung) bits.push("Lung:" + session.lung);
    if (session.extremities) bits.push("Ext:" + session.extremities);
    if (session.objectiveOther) bits.push(session.objectiveOther);
    return bits.join(" · ");
  }

  function renderChecklistPatientHtml(data, scale) {
    const root = document.createElement("div");
    root.className = "dense-checklist-patient";
    const p = (data && data.patient) || {};
    const cells = [
      ["Patient name:", p.name],
      ["DOB:", p.birthDateLabel],
      ["HN:", p.hospitalNumber],
      ["Sessions per week:", p.sessionsPerWeekLabel],
      ["Dialysis days:", p.dialysisDays],
      ["Coverage scheme:", p.coverageScheme],
      ["Dialysis mode:", p.dialysisMode],
      ["Underlying:", p.underlying],
    ];
    root.style.display = "grid";
    root.style.gridTemplateColumns = "1fr 1fr 1fr 1fr";
    root.style.height = "100%";
    root.style.fontSize = Math.max(7, 9 * Math.min(1, scale / 3.2)) + "px";
    cells.forEach((pair) => {
      const cell = document.createElement("div");
      cell.className = "dense-checklist-cell";
      cell.innerHTML = `<strong>${escapeHtml(pair[0])}</strong> ${escapeHtml(pair[1] || "—")}`;
      root.appendChild(cell);
    });
    return root;
  }

  /** Port of HprpMatrixColumnPlan — 2 zones (item + month-band) → physical columns. */
  function parseMixedColumnToken(raw) {
    let token = String(raw == null ? "" : raw).trim();
    if (!token) return null;
    let constant = false;
    if (/mm$/i.test(token)) {
      constant = true;
      token = token.slice(0, -2).trim();
    } else if (token === "*") {
      token = "1";
    }
    const value = Number(token);
    if (!Number.isFinite(value) || value <= 0) return null;
    return { constantMm: constant, value: value };
  }

  function formatMatrixWidthToken(zone) {
    if (!zone) return "*";
    if (zone.constantMm) {
      const n = Math.round(zone.value * 100) / 100;
      return n + "mm";
    }
    if (Math.abs(zone.value - 1) < 0.001) return "*";
    return String(Math.round(zone.value * 100) / 100);
  }

  function resolveMatrixColumnPlan(columnWidths, monthCount) {
    const months = Math.max(0, Number(monthCount) || 0);
    let zones = (columnWidths || []).map(parseMixedColumnToken).filter(Boolean);
    if (zones.length < 2) {
      zones = [parseMixedColumnToken("46mm"), parseMixedColumnToken("*")];
    }
    const item = zones[0];
    const band = zones[1];
    const cols = [{ constantMm: item.constantMm, value: item.value }];
    if (months === 0) return { zones: [item, band], columns: cols };
    const per = Math.max(0.1, band.value / months);
    for (let i = 0; i < months; i++) {
      cols.push({ constantMm: band.constantMm, value: per });
    }
    return { zones: [item, band], columns: cols };
  }

  function matrixColgroupCss(plan, tableWidthMm) {
    const cols = plan.columns || [];
    const wMm = Math.max(1, Number(tableWidthMm) || 269);
    let fixed = 0;
    let rel = 0;
    cols.forEach((c) => {
      if (c.constantMm) fixed += c.value;
      else rel += c.value;
    });
    const remain = Math.max(0.1, wMm - fixed);
    return cols.map((c) => {
      if (c.constantMm) {
        const pct = (c.value / wMm) * 100;
        return Math.max(0.5, pct).toFixed(3) + "%";
      }
      const share = rel > 0 ? (c.value / rel) * remain : remain / Math.max(1, cols.length);
      return Math.max(0.5, (share / wMm) * 100).toFixed(3) + "%";
    });
  }

  function readMatrixZoneTokens(el) {
    const chrome = mergeTableChrome(el);
    const widths = (chrome && chrome.columnWidths) || ["46mm", "*"];
    const item = parseMixedColumnToken(widths[0]) || parseMixedColumnToken("46mm");
    const band = parseMixedColumnToken(widths[1]) || parseMixedColumnToken("*");
    return { chrome: chrome, item: item, band: band, widths: widths };
  }

  function writeMatrixZoneTokens(el, working, itemZone, bandZone) {
    el.chrome = mergeTableChrome(el);
    el.chrome.columnWidths = [formatMatrixWidthToken(itemZone), formatMatrixWidthToken(bandZone)];
    if (working) {
      working.chrome = working.chrome || {};
      working.chrome.columnWidths = el.chrome.columnWidths.slice();
    }
  }

  function renderChecklistGridHtml(el, data, scale) {
    const root = document.createElement("div");
    root.className = "dense-checklist-grid";
    root.style.height = "100%";
    root.style.position = "relative";
    const title = document.createElement("div");
    title.className = "dense-checklist-grid-title";
    title.textContent = "Check lists";
    root.appendChild(title);

    const columns = (data && data.columns) || [];
    const yearSpans = (data && data.yearSpans) || [];
    const items = (data && data.checklistItems) || [];
    if (!columns.length || !items.length) {
      const empty = document.createElement("div");
      empty.className = "ph-dense";
      empty.textContent = "No progress note data available for the selected range.";
      root.appendChild(empty);
      return root;
    }

    const plan = resolveMatrixColumnPlan(
      (el.chrome && el.chrome.columnWidths)
        || (el.tablePreset && el.tablePreset.chrome && el.tablePreset.chrome.columnWidths),
      columns.length);
    const tableW = Math.max(1, Number(el.box && el.box.wMm) || 269);
    const cssWidths = matrixColgroupCss(plan, tableW);

    const table = document.createElement("table");
    table.className = "dense-checklist-table dense-checklist-table-plan";
    const colgroup = document.createElement("colgroup");
    cssWidths.forEach((w) => {
      const col = document.createElement("col");
      col.style.width = w;
      colgroup.appendChild(col);
    });
    table.appendChild(colgroup);

    const thead = document.createElement("thead");
    const yearRow = document.createElement("tr");
    yearRow.appendChild(document.createElement("th"));
    yearSpans.forEach((span) => {
      const th = document.createElement("th");
      th.colSpan = Math.max(1, Number(span.colSpan) || 1);
      th.textContent = String(span.year || "");
      yearRow.appendChild(th);
    });
    thead.appendChild(yearRow);
    const monthRow = document.createElement("tr");
    const itemTh = document.createElement("th");
    itemTh.textContent = "Item";
    monthRow.appendChild(itemTh);
    columns.forEach((c) => {
      const th = document.createElement("th");
      th.textContent = String(c.calendarMonth || "");
      monthRow.appendChild(th);
    });
    thead.appendChild(monthRow);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");
    let lastGroup = null;
    items.forEach((item) => {
      if (item.group && item.group !== lastGroup) {
        lastGroup = item.group;
        const gr = document.createElement("tr");
        const gd = document.createElement("td");
        gd.colSpan = columns.length + 1;
        gd.className = "dense-checklist-group";
        gd.textContent = item.group;
        gr.appendChild(gd);
        tbody.appendChild(gr);
      }
      const tr = document.createElement("tr");
      const label = document.createElement("td");
      label.textContent = item.label || "";
      tr.appendChild(label);
      const marks = item.marks || [];
      for (let i = 0; i < columns.length; i++) {
        const td = document.createElement("td");
        td.textContent = marks[i] || "";
        tr.appendChild(td);
      }
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    root.appendChild(table);
    attachMatrixZoneResizer(root, table, el, scale);
    void scale;
    return root;
  }

  /** Drag the Item | Month-band boundary (2-zone chrome.columnWidths). */
  function attachMatrixZoneResizer(root, table, el, scale) {
    const working = resolveTablePreset(el);
    requestAnimationFrame(() => {
      const itemTh = table.querySelector("thead tr:last-child th");
      if (!itemTh) return;
      const overlay = document.createElement("div");
      overlay.className = "col-resize-layer";
      root.appendChild(overlay);
      const rootRect = root.getBoundingClientRect();
      const rect = itemTh.getBoundingClientRect();
      const handle = document.createElement("div");
      handle.className = "col-resize";
      handle.style.left = (rect.right - rootRect.left - 3) + "px";
      handle.title = "ลากปรับ Item ↔ แถบเดือน (columnWidths)";
      handle.addEventListener("pointerdown", (e) => {
        e.preventDefault();
        e.stopPropagation();
        startMatrixZoneResize(e, el, working, table, handle, root, scale);
      });
      overlay.appendChild(handle);
    });
  }

  function startMatrixZoneResize(e, el, working, table, handle, root, scale) {
    if (canvasTools) canvasTools.pushHistory();
    const startX = e.clientX;
    const zones = readMatrixZoneTokens(el);
    const tableW = Math.max(1, Number(el.box && el.box.wMm) || 269);
    const itemTh = table.querySelector("thead tr:last-child th");
    const tableRect = table.getBoundingClientRect();
    const startItemPx = itemTh ? itemTh.getBoundingClientRect().width : tableRect.width * 0.2;
    const pxPerMm = scale > 0 ? scale : (tableRect.width / tableW);

    function onMove(ev) {
      const dxPx = ev.clientX - startX;
      let itemMm = (startItemPx + dxPx) / Math.max(0.01, pxPerMm);
      itemMm = Math.max(20, Math.min(tableW - 20, itemMm));
      const bandMm = Math.max(10, tableW - itemMm);
      const itemShare = itemMm / tableW;
      const bandShare = bandMm / tableW;

      let nextItem = Object.assign({}, zones.item);
      let nextBand = Object.assign({}, zones.band);

      if (zones.item.constantMm && zones.band.constantMm) {
        nextItem.value = Math.round(itemMm * 10) / 10;
        nextBand.value = Math.round(bandMm * 10) / 10;
      } else if (zones.item.constantMm && !zones.band.constantMm) {
        nextItem.value = Math.round(itemMm * 10) / 10;
      } else if (!zones.item.constantMm && zones.band.constantMm) {
        nextBand.value = Math.round(bandMm * 10) / 10;
      } else {
        const pair = Math.max(0.2, zones.item.value + zones.band.value);
        nextItem.value = Math.max(0.1, pair * itemShare);
        nextBand.value = Math.max(0.1, pair * bandShare);
      }

      writeMatrixZoneTokens(el, working, nextItem, nextBand);
      if (working) commitWorking(el, working);

      const plan = resolveMatrixColumnPlan(el.chrome.columnWidths, (sampleData && sampleData.columns || []).length || 12);
      const css = matrixColgroupCss(plan, tableW);
      const cols = table.querySelectorAll("colgroup col");
      css.forEach((w, i) => { if (cols[i]) cols[i].style.width = w; });
      if (itemTh && handle && root) {
        const rootRect = root.getBoundingClientRect();
        const rect = itemTh.getBoundingClientRect();
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
      }
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
      if (setStatusRef) setStatusRef("Matrix columnWidths อัปเดต — Download PDF จะใช้ชุดนี้", "ok");
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  function renderChecklistTextNotesHtml(data, scale) {
    const root = document.createElement("div");
    root.className = "dense-checklist-notes";
    const notes = (data && data.textNotes) || [];
    if (!notes.length) {
      root.innerHTML = `<div class="ph-dense">text notes (empty)</div>`;
      return root;
    }
    root.innerHTML = `<div class="dense-checklist-grid-title">Text note</div>`;
    notes.forEach((n) => {
      const block = document.createElement("div");
      block.className = "dense-checklist-note";
      block.innerHTML = `<strong>${escapeHtml(n.monthLabel || "")}</strong><div>${escapeHtml(n.content || "")}</div>`;
      root.appendChild(block);
    });
    void scale;
    return root;
  }

  function renderHeaderHtml(model, el, scale) {
    const root = document.createElement("div");
    root.className = "cfg-header" + (borderOn(el.chrome) || borderOn(model.preset.chrome) ? "" : " cfg-no-border");
    const preset = model.preset;
    const cols = preset.columns || [];
    const wMm = Math.max(1, el.box.wMm);
    const fracs = global.HeaderLayoutEngine.bandFractions(cols, wMm);
    const titlePx = Math.max(8, model.titleRowHeightMm * scale);
    const bottomPx = Math.max(0, model.bottomRowHeightMm * scale);
    root.style.height = "100%";

    const top = document.createElement("div");
    top.className = "cfg-header-top";
    top.style.height = titlePx.toFixed(2) + "px";
    top.style.display = "grid";
    top.style.gridTemplateColumns = fracs.map((f) => (f * 100).toFixed(3) + "%").join(" ");

    cols.forEach((band, bi) => {
      const cell = document.createElement("div");
      cell.className = "cfg-header-band kind-" + String(band.kind || "title").toLowerCase();
      const kind = String(band.kind || "").toLowerCase();
      if (kind === "logo") {
        cell.textContent = model.logoFallbackText || "Logo";
      } else if (kind === "title") {
        cell.classList.add("cfg-header-title");
        cell.textContent = nbspOr(model.titleText);
      } else if (kind === "meta") {
        const meta = document.createElement("div");
        meta.className = "cfg-header-meta";
        const lineH = titlePx / Math.max(1, model.metaLines.length);
        model.metaLines.forEach((line) => {
          const row = document.createElement("div");
          row.className = "cfg-header-meta-line";
          row.style.height = lineH.toFixed(2) + "px";
          let html = `<strong>${escapeHtml(line.label)}</strong> ${escapeHtml(nbspOr(line.value))}`;
          if (line.label2) {
            html += ` <strong>${escapeHtml(line.label2)}</strong> ${escapeHtml(nbspOr(line.value2))}`;
          }
          row.innerHTML = html;
          meta.appendChild(row);
        });
        cell.appendChild(meta);
      } else {
        cell.textContent = band.id;
      }
      top.appendChild(cell);
      if (bi < cols.length - 1) {
        // resizer attached after layout
      }
    });
    root.appendChild(top);

    if (bottomPx > 0.5 && (model.bottomFields || []).length) {
      const bottom = document.createElement("div");
      bottom.className = "cfg-header-bottom";
      bottom.style.height = bottomPx.toFixed(2) + "px";
      const rowCount = Math.max(1, Number(model.bottomRowCount) || 1);
      if (rowCount <= 1) {
        const fieldsWrap = document.createElement("div");
        fieldsWrap.className = "cfg-header-bottom-fields";
        model.bottomFields.forEach((f) => {
          const span = document.createElement("span");
          span.className = "cfg-header-field";
          span.innerHTML = `<strong>${escapeHtml(f.label)}</strong> ${escapeHtml(nbspOr(f.value))}`;
          fieldsWrap.appendChild(span);
        });
        if (model.showDateAndHdNo) {
          const date = document.createElement("span");
          date.className = "cfg-header-field";
          date.innerHTML = `<strong>Date</strong> ${escapeHtml(nbspOr(model.dateText))} <strong>HD NO.</strong> ${escapeHtml(nbspOr(model.hdNoText))}`;
          fieldsWrap.appendChild(date);
        }
        bottom.appendChild(fieldsWrap);
      } else {
        bottom.classList.add("cfg-header-bottom-multi");
        bottom.style.display = "grid";
        bottom.style.gridTemplateRows = "repeat(" + rowCount + ", 1fr)";
        for (let ri = 0; ri < rowCount; ri++) {
          const fieldsWrap = document.createElement("div");
          fieldsWrap.className = "cfg-header-bottom-fields";
          model.bottomFields.filter((f) => (Number(f.row) || 0) === ri).forEach((f) => {
            const span = document.createElement("span");
            span.className = "cfg-header-field";
            span.innerHTML = `<strong>${escapeHtml(f.label)}</strong> ${escapeHtml(nbspOr(f.value))}`;
            fieldsWrap.appendChild(span);
          });
          bottom.appendChild(fieldsWrap);
        }
      }
      root.appendChild(bottom);
    }

    attachHeaderBandResizers(root, top, el, preset, fracs, wMm);
    return root;
  }

  function attachHeaderBandResizers(root, topRow, el, preset, fracs, wMm) {
    const working = ensureWorkingHeaderPreset(el, preset);
    requestAnimationFrame(() => {
      const bands = topRow.querySelectorAll(".cfg-header-band");
      if (bands.length < 2) return;
      const overlay = document.createElement("div");
      overlay.className = "col-resize-layer";
      root.appendChild(overlay);
      const rootRect = root.getBoundingClientRect();
      bands.forEach((band, hi) => {
        if (hi >= bands.length - 1) return;
        const rect = band.getBoundingClientRect();
        const handle = document.createElement("div");
        handle.className = "col-resize";
        handle.style.left = (rect.right - rootRect.left - 3) + "px";
        handle.style.height = topRow.style.height;
        handle.title = "ลากปรับความกว้างแถบ header (ส่งผลต่อ PDF)";
        handle.addEventListener("pointerdown", (e) => {
          e.preventDefault();
          e.stopPropagation();
          startHeaderBandResize(e, el, working, hi, fracs.slice(), wMm);
        });
        overlay.appendChild(handle);
      });
    });
  }

  function startHeaderBandResize(e, el, working, index, fracs, wMm) {
    if (canvasTools) canvasTools.pushHistory();
    const startX = e.clientX;
    const leftF = fracs[index];
    const rightF = fracs[index + 1];
    const pair = leftF + rightF;
    const metrics = pageMetrics();

    function onMove(ev) {
      const dxMm = (ev.clientX - startX) / metrics.scale;
      const dFrac = dxMm / Math.max(1, wMm);
      let newLeft = Math.max(0.05, leftF + dFrac);
      let newRight = Math.max(0.05, pair - newLeft);
      fracs[index] = newLeft;
      fracs[index + 1] = newRight;
      applyBandFractionsToPreset(working, fracs, wMm);
      commitHeaderWorking(el, working);
      selectedElementId = el.id;
      const top = document.querySelector('.designer-element[data-element-id="' + el.id + '"] .cfg-header-top');
      if (top) {
        top.style.gridTemplateColumns = fracs.map((f) => (f * 100).toFixed(3) + "%").join(" ");
      }
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
      if (setStatusRef) setStatusRef("Header columns อัปเดต — Download PDF จะใช้ความกว้างชุดนี้", "ok");
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  /** Persist fractions as absolute mm widths (QuestPDF ConstantItem) for PDF parity. */
  function applyBandFractionsToPreset(working, fracs, wMm) {
    (working.columns || []).forEach((c, i) => {
      c.widthMm = Math.round(fracs[i] * wMm * 10) / 10;
      c.weight = 1;
    });
  }

  function startMoveDrag(e, el, wrap, sheet, metrics) {
    const els = stateRef.draft.layout.elements;
    const fromIndex = els.indexOf(el);
    dragState = { type: "move", el, fromIndex, startX: e.clientX, startY: e.clientY };
    wrap.classList.add("dragging");
    const hint = document.getElementById("designerDropHint");

    function onMove(ev) {
      const dx = Math.abs(ev.clientX - dragState.startX);
      const dy = Math.abs(ev.clientY - dragState.startY);
      if (dx + dy < 4) return;
      dragState.moved = true;
      const target = hitTestElement(ev.clientX, ev.clientY, el.id);
      if (hint) {
        hint.classList.remove("hidden");
        if (!target) {
          hint.textContent = "วางต่อล่าง (ท้ายรายการ)";
          hint.dataset.mode = "end";
        } else {
          const tRect = target.getBoundingClientRect();
          const beside = ev.clientX > tRect.left + tRect.width * 0.55;
          hint.textContent = beside ? "วางข้าง →" : "วางต่อล่าง ↓";
          hint.dataset.mode = beside ? "beside" : "below";
          hint.dataset.targetId = target.dataset.elementId;
          hint.style.left = (tRect.left - sheet.getBoundingClientRect().left) + "px";
          hint.style.top = (tRect.top - sheet.getBoundingClientRect().top - 22) + "px";
        }
      }
    }

    function onUp(ev) {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      wrap.classList.remove("dragging");
      if (hint) hint.classList.add("hidden");
      if (!dragState.moved) {
        dragState = null;
        // Click-select (no drag): keep inspector in sync with canvas selection.
        renderInspector();
        return;
      }
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);

      const target = hitTestElement(ev.clientX, ev.clientY, el.id);
      reorderElement(el, target, hint && hint.dataset.mode);
      dragState = null;
      renderAll();
    }

    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  function hitTestElement(clientX, clientY, excludeId) {
    const nodes = document.querySelectorAll(".designer-element");
    for (const n of nodes) {
      if (n.dataset.elementId === excludeId) continue;
      const r = n.getBoundingClientRect();
      if (clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom)
        return n;
    }
    return null;
  }

  function reorderElement(el, targetNode, mode) {
    if (canvasTools) canvasTools.pushHistory();
    // Lift out of a column stack when reordering at top level.
    const fromLoc = findElementLocation(el.id);
    if (!fromLoc) return;

    if (fromLoc.parent && isGroup(fromLoc.parent)) {
      fromLoc.list.splice(fromLoc.index, 1);
      if (!fromLoc.parent.children.length) {
        const gLoc = findElementLocation(fromLoc.parent.id);
        if (gLoc && !gLoc.parent) {
          const besideFix = gLoc.list[gLoc.index + 1];
          gLoc.list.splice(gLoc.index, 1);
          if (besideFix && String(besideFix.place).toLowerCase() === "beside")
            besideFix.place = "below";
        }
      }
    } else {
      fromLoc.list.splice(fromLoc.index, 1);
    }

    const els = stateRef.draft.layout.elements;

    if (!targetNode || mode === "end") {
      el.place = "below";
      els.push(el);
      reflowElements();
      return;
    }

    const targetId = targetNode.dataset.elementId;
    const toLoc = findElementLocation(targetId);
    if (!toLoc) {
      el.place = "below";
      els.push(el);
      reflowElements();
      return;
    }

    // Drop onto a stack child → insert into that column (inner below / beside→below in stack).
    if (toLoc.parent && isGroup(toLoc.parent)) {
      if (toLoc.parent.children.length >= MAX_GROUP_CHILDREN && !toLoc.parent.children.includes(el)) {
        el.place = "below";
        els.push(el);
        reflowElements();
        if (setStatusRef) setStatusRef("คอลัมน์เต็ม (max " + MAX_GROUP_CHILDREN + ") — วางเป็นแถวนอก", "err");
        return;
      }
      el.place = "below";
      const insertAt = mode === "beside" ? toLoc.index + 1 : toLoc.index + 1;
      toLoc.parent.children.splice(insertAt, 0, el);
      reflowElements();
      return;
    }

    const to = els.findIndex((e) => e.id === targetId);
    if (to < 0) {
      el.place = "below";
      els.push(el);
      reflowElements();
      return;
    }

    if (mode === "beside") {
      el.place = "beside";
      els.splice(to + 1, 0, el);
    } else {
      el.place = "below";
      els.splice(to + 1, 0, el);
    }
    reflowElements();
  }

  function startResizeDrag(e, el, dir, metrics) {
    if (canvasTools) canvasTools.pushHistory();
    const startX = e.clientX;
    const startY = e.clientY;
    const startW = el.box.wMm;
    const startH = el.box.hMm;
    const { contentW, margins, gaps } = metrics;
    const loc0 = findElementLocation(el.id);
    const widthTarget = loc0 && loc0.parent && isGroup(loc0.parent) ? loc0.parent : el;
    const startWidthTarget = Number(widthTarget.box.wMm) || startW;

    // If east-resizing a block that has a beside neighbor to the right, treat the
    // shared edge as a splitter: keep the pair's total width constant so the
    // neighbor can reclaim space (fixes "can't drag back to the right").
    let pairSibling = null;
    let pairStartW = 0;
    let pairTotal = 0;
    if ((dir === "e" || dir === "se") && !(loc0 && loc0.parent)) {
      const els = stateRef.draft.layout.elements;
      const idx = els.indexOf(widthTarget);
      if (idx >= 0 && idx + 1 < els.length
        && String(els[idx + 1].place || "below").toLowerCase() === "beside") {
        pairSibling = els[idx + 1];
        pairStartW = Math.max(MIN_BLOCK_W, Number(pairSibling.box && pairSibling.box.wMm) || MIN_BLOCK_W);
        pairTotal = startWidthTarget + pairStartW;
      }
    }

    const frozenMaxW = (dir === "e" || dir === "se")
      ? (pairSibling
        ? Math.max(MIN_BLOCK_W, pairTotal - MIN_BLOCK_W)
        : maxWidthInRow(widthTarget, contentW, margins, gaps, metrics.scale))
      : contentW;

    function onMove(ev) {
      const dx = (ev.clientX - startX) / metrics.scale;
      const dy = (ev.clientY - startY) / metrics.scale;
      if (dir === "e" || dir === "se") {
        if (pairSibling) {
          let nextW = Math.max(MIN_BLOCK_W, Math.min(frozenMaxW, startWidthTarget + dx));
          let sibW = pairTotal - nextW;
          if (sibW < MIN_BLOCK_W) {
            sibW = MIN_BLOCK_W;
            nextW = pairTotal - sibW;
          }
          widthTarget.box.wMm = nextW;
          widthTarget.manualWidth = true;
          pairSibling.box.wMm = sibW;
          pairSibling.manualWidth = true;
        } else {
          widthTarget.box.wMm = Math.max(MIN_BLOCK_W, Math.min(frozenMaxW, startWidthTarget + dx));
          widthTarget.manualWidth = true;
        }
      }
      if (dir === "s" || dir === "se") {
        el.box.hMm = Math.max(minHeightForElement(el), startH + dy);
      }
      reflowElements();
      renderCanvas();
      selectedElementId = el.id;
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  function deleteElement(id) {
    ensureElements();
    if (canvasTools) canvasTools.pushHistory();
    const loc = findElementLocation(id);
    if (!loc) return;
    loc.list.splice(loc.index, 1);
    if (loc.parent && isGroup(loc.parent)) {
      if (!loc.parent.children.length) {
        const gLoc = findElementLocation(loc.parent.id);
        if (gLoc && !gLoc.parent) {
          const besideFix = gLoc.list[gLoc.index + 1];
          gLoc.list.splice(gLoc.index, 1);
          if (besideFix && String(besideFix.place).toLowerCase() === "beside")
            besideFix.place = "below";
        }
      }
    } else {
      const els = stateRef.draft.layout.elements;
      if (els[loc.index] && String(els[loc.index].place).toLowerCase() === "beside") {
        els[loc.index].place = "below";
      }
    }
    if (selectedElementId === id) selectedElementId = null;
    selectedElementIds.delete(id);
    if (!selectedElementIds.has(selectedElementId)) {
      selectedElementId = selectedElementIds.size
        ? selectedElementIds.values().next().value
        : null;
    }
    reflowElements();
    renderAll();
    if (setStatusRef) setStatusRef("ลบ widget แล้ว", "ok");
  }

  function startStackSplitDrag(e, groupId, splitIndex, metrics) {
    const gLoc = findElementLocation(groupId);
    if (!gLoc || !isGroup(gLoc.el)) return;
    const kids = gLoc.el.children || [];
    if (splitIndex < 0 || splitIndex >= kids.length - 1) return;
    if (canvasTools) canvasTools.pushHistory();
    const above = kids[splitIndex];
    const below = kids[splitIndex + 1];
    const startY = e.clientY;
    const startAboveH = Number(above.box.hMm) || minHeightForElement(above);
    const startBelowH = Number(below.box.hMm) || minHeightForElement(below);
    const minA = minHeightForElement(above);
    const minB = minHeightForElement(below);
    const total = startAboveH + startBelowH;

    function onMove(ev) {
      const dy = (ev.clientY - startY) / metrics.scale;
      let nextAbove = startAboveH + dy;
      nextAbove = Math.max(minA, Math.min(total - minB, nextAbove));
      above.box.hMm = nextAbove;
      below.box.hMm = total - nextAbove;
      reflowElements();
      renderCanvas();
    }
    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      suppressClick = true;
      setTimeout(() => { suppressClick = false; }, 0);
      renderAll();
    }
    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  /**
   * Insert element below selection inside a column stack (wrap into group if needed).
   * Soft-capped at MAX_GROUP_CHILDREN.
   */
  function insertElementInnerBelow(newEl) {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    newEl.band = newEl.band || "content";
    newEl.place = "below";
    newEl.box = newEl.box || { xMm: 0, yMm: 0, wMm: 80, hMm: 40 };

    const selId = selectedElementId;
    const loc = selId ? findElementLocation(selId) : null;
    if (!loc) {
      newEl.place = "below";
      stateRef.draft.layout.elements.push(newEl);
      setSingleSelection(newEl.id);
      reflowElements();
      renderAll();
      if (setStatusRef) setStatusRef("ไม่มี selection — แทรกเป็นแถวนอก", "ok");
      return;
    }

    if (loc.parent && isGroup(loc.parent)) {
      if (loc.parent.children.length >= MAX_GROUP_CHILDREN) {
        if (setStatusRef)
          setStatusRef("คอลัมน์นี้มีครบ " + MAX_GROUP_CHILDREN + " ชิ้นแล้ว (inner stack max)", "err");
        return;
      }
      loc.parent.children.splice(loc.index + 1, 0, newEl);
      setSingleSelection(newEl.id);
      reflowElements();
      renderAll();
      if (setStatusRef) setStatusRef("แทรก inner below ใน column stack", "ok");
      return;
    }

    if (isGroup(loc.el)) {
      if ((loc.el.children || []).length >= MAX_GROUP_CHILDREN) {
        if (setStatusRef)
          setStatusRef("คอลัมน์นี้มีครบ " + MAX_GROUP_CHILDREN + " ชิ้นแล้ว (inner stack max)", "err");
        return;
      }
      loc.el.children = loc.el.children || [];
      loc.el.children.push(newEl);
      setSingleSelection(newEl.id);
      reflowElements();
      renderAll();
      if (setStatusRef) setStatusRef("แทรกท้าย column stack", "ok");
      return;
    }

    // Wrap selected leaf into a new group and append newEl.
    const selected = loc.el;
    const groupId = "grp_" + Math.random().toString(36).slice(2, 7);
    const group = {
      id: groupId,
      type: "group",
      direction: "column",
      band: resolveBand(selected) || "content",
      place: selected.place || "below",
      manualWidth: !!selected.manualWidth,
      box: {
        xMm: 0,
        yMm: 0,
        wMm: Number(selected.box && selected.box.wMm) || 80,
        hMm: Number(selected.box && selected.box.hMm) || 40,
      },
      children: [selected, newEl],
    };
    selected.place = "below";
    loc.list[loc.index] = group;
    setSingleSelection(newEl.id);
    stateRef.draft.manifest.layoutMode = "designer";
    reflowElements();
    renderAll();
    if (setStatusRef) setStatusRef("สร้าง column stack + แทรก inner below", "ok");
  }

  function renderInspector() {
    const insp = elsRef.inspector;
    if (!insp || !isStudioCanvas()) return;
    insp.innerHTML = "";

    if (stateRef.selectedKey === "page") {
      renderPageInspector(insp);
      return;
    }
    if (stateRef.selectedKey === "labels") {
      insp.innerHTML = "<p class=\"muted\">ใช้แท็บ JSON → labels สำหรับข้อความละเอียด</p>";
      return;
    }

    const el = selectedElement();
    if (!el) {
      insp.innerHTML = "<p class=\"muted\">คลิก block บน canvas · Shift+คลิกเลือกหลายชิ้น · Add to library</p>";
      return;
    }

    const head = document.createElement("div");
    head.className = "insp-head";
    head.innerHTML = `<strong>${escapeHtml(el.type)}</strong> <span class="muted">${escapeHtml(el.id)}</span>`;
    insp.appendChild(head);

    if (selectedElementIds.size > 1) {
      const multiNote = document.createElement("p");
      multiNote.className = "muted";
      multiNote.textContent = selectedElementIds.size
        + " selected · Shift+คลิกเพิ่ม/ถอด · Add to library จะบันทึกเป็น Fragment";
      insp.appendChild(multiNote);
    }

    const del = document.createElement("button");
    del.type = "button";
    del.className = "danger-btn";
    del.textContent = "ลบ widget";
    del.addEventListener("click", () => deleteElement(el.id));
    insp.appendChild(del);

    const placeLab = document.createElement("label");
    placeLab.textContent = "วาง (place)";
    const placeSel = document.createElement("select");
    [["below", "ต่อล่าง"], ["beside", "ต่อข้าง"]].forEach(([v, t]) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = t;
      if (String(el.place || "below") === v) o.selected = true;
      placeSel.appendChild(o);
    });
    placeSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.place = placeSel.value;
      reflowElements();
      renderAll();
    });
    placeLab.appendChild(placeSel);
    insp.appendChild(placeLab);

    const bandLab = document.createElement("label");
    bandLab.textContent = "โซนหน้า (band)";
    const bandSel = document.createElement("select");
    [
      ["content", "Content (ไหลหลายหน้า)"],
      ["header", "Header (ซ้ำทุกหน้า)"],
      ["super-header", "Super header (ชื่อรายงาน ฯลฯ)"],
      ["footer", "Footer (ซ้ำทุกหน้า)"],
      ["super-footer", "Super footer (Page of ฯลฯ)"],
    ].forEach(([v, t]) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = t;
      if (resolveBand(el) === v) o.selected = true;
      bandSel.appendChild(o);
    });
    bandSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.band = bandSel.value;
      reflowElements();
      renderAll();
    });
    bandLab.appendChild(bandSel);
    insp.appendChild(bandLab);
    const bandHint = document.createElement("p");
    bandHint.className = "muted";
    bandHint.textContent = "Header/Footer ซ้ำทุกหน้า · Content เกินสูงสุดจะเปิดหน้าถัดไปอัตโนมัติ";
    insp.appendChild(bandHint);

    const borderLab = document.createElement("label");
    borderLab.className = "check-lab";
    const borderCb = document.createElement("input");
    borderCb.type = "checkbox";
    borderCb.checked = borderOn(el.chrome);
    borderCb.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.chrome = el.chrome || {};
      el.chrome.border = borderCb.checked ? "thin" : "none";
      renderAll();
    });
    borderLab.appendChild(borderCb);
    borderLab.appendChild(document.createTextNode(" เส้นขอบ block"));
    insp.appendChild(borderLab);

    const fitBtn = document.createElement("button");
    fitBtn.type = "button";
    fitBtn.textContent = "กว้างพอดีขอบ (auto width)";
    fitBtn.addEventListener("click", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.manualWidth = false;
      reflowElements();
      renderAll();
    });
    insp.appendChild(fitBtn);

    const fillRowBtn = document.createElement("button");
    fillRowBtn.type = "button";
    fillRowBtn.textContent = "กว้างเต็มแถว (fill row)";
    fillRowBtn.title = "ขยาย block นี้ให้เต็มพื้นที่แนวนอนที่เหลือในแถว";
    fillRowBtn.addEventListener("click", () => {
      if (canvasTools) canvasTools.pushHistory();
      const m = pageMetrics();
      el.box.wMm = maxWidthInRow(el, m.contentW, m.margins, m.gaps, m.scale);
      el.manualWidth = true;
      reflowElements();
      renderAll();
    });
    insp.appendChild(fillRowBtn);

    if (el.type === "config-table") {
      renderTableInspector(insp, el);
    }
    if (el.type === "data-grid") {
      renderDataGridInspector(insp, el);
    }
    if (el.type === "header") {
      renderHeaderInspector(insp, el);
    }
    if (el.type === "box-text") {
      renderBoxTextInspector(insp, el);
    }
    if (el.type === "narrative") {
      renderNarrativeInspector(insp, el);
    }
    if (el.type === "page-of") {
      renderPageOfInspector(insp, el);
    }

    renderBoxFields(insp, el);
  }

  function resolveBoxText(el, data) {
    if (Array.isArray(el.items) && el.items.length) {
      return el.items.map((item) => formatBoxTextItemPlain(item, data)).filter(Boolean).join("  ");
    }
    if (el.bind && data && global.HeaderLayoutEngine) {
      const v = global.HeaderLayoutEngine.readAt(data, el.bind);
      if (v != null && String(v).trim() !== "") return String(v);
    }
    return el.text || "";
  }

  function resolveBoxItemValue(bind, text, data) {
    if (bind && data && global.HeaderLayoutEngine) {
      const v = global.HeaderLayoutEngine.readAt(data, bind);
      if (v != null && String(v).trim() !== "") return String(v);
    }
    return text && String(text).trim() ? String(text) : "";
  }

  function formatBoxTextItemPlain(item, data) {
    const parts = [];
    const v1 = resolveBoxItemValue(item.bind, item.text, data);
    const v2 = resolveBoxItemValue(item.bind2, item.text2, data);
    if (item.label) parts.push(String(item.label).trim());
    if (v1) parts.push(v1);
    if (item.label2) parts.push(String(item.label2).trim());
    if (v2) parts.push(v2);
    return parts.join(" ");
  }

  function appendBoxTextItemSpans(host, item, data) {
    function addLabel(text) {
      if (!text) return;
      const s = document.createElement("span");
      s.className = "cfg-box-text-label";
      s.textContent = String(text).trimEnd() + " ";
      host.appendChild(s);
    }
    function addValue(text) {
      if (!text) return;
      const s = document.createElement("span");
      s.className = "cfg-box-text-value";
      s.textContent = text;
      host.appendChild(s);
    }
    addLabel(item.label);
    addValue(resolveBoxItemValue(item.bind, item.text, data));
    addLabel(item.label2);
    addValue(resolveBoxItemValue(item.bind2, item.text2, data));
    if (!host.childNodes.length) host.textContent = "\u00A0";
  }

  function renderBoxTextHtml(el, data, scale) {
    const root = document.createElement("div");
    root.className = "cfg-box-text" + (borderOn(el.chrome) ? "" : " cfg-no-border");
    const fill = (el.chrome && el.chrome.headerFill) || "#c8b8e8";
    if (String(fill).indexOf("$") !== 0) root.style.background = fill;
    else root.style.background = "#c8b8e8";
    const fs = (el.chrome && el.chrome.fontSize) || 7.5;
    root.style.fontSize = (fs * (scale / 2.5)).toFixed(1) + "px";
    root.style.height = "100%";

    if (Array.isArray(el.items) && el.items.length) {
      root.classList.add("cfg-box-text-multi");
      el.items.forEach((item) => {
        const seg = document.createElement("div");
        seg.className = "cfg-box-text-item";
        const flex = item.flex != null && Number(item.flex) > 0 ? Number(item.flex) : 1;
        seg.style.flex = String(flex);
        const align = String(item.align || "left").toLowerCase();
        seg.style.justifyContent =
          align === "right" ? "flex-end" : align === "center" ? "center" : "flex-start";
        seg.style.textAlign = align;
        appendBoxTextItemSpans(seg, item, data);
        root.appendChild(seg);
      });
      return root;
    }

    root.style.textAlign = el.align || "center";
    root.style.justifyContent =
      el.align === "left" ? "flex-start" : el.align === "right" ? "flex-end" : "center";
    root.textContent = resolveBoxText(el, data) || "\u00A0";
    return root;
  }

  function formatPageOf(el, current, total) {
    const fmt = el.text || "{current} / {total}";
    return String(fmt)
      .replace(/\{current\}/gi, String(current))
      .replace(/\{total\}/gi, String(total));
  }

  function renderPageOfHtml(el, current, total, scale) {
    const root = document.createElement("div");
    root.className = "cfg-page-of" + (borderOn(el.chrome) ? "" : " cfg-no-border");
    const fs = (el.chrome && el.chrome.fontSize) || 8;
    root.style.fontSize = (fs * (scale / 2.5)).toFixed(1) + "px";
    root.style.textAlign = el.align || "center";
    root.style.height = "100%";
    root.textContent = formatPageOf(el, current, total);
    return root;
  }

  function renderPageOfInspector(insp, el) {
    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent = "Page of — วางที่ Super footer (นอกเส้น margin) · ใช้ {current} และ {total}";
    insp.appendChild(tip);

    if (!el.band) el.band = "super-footer";

    const textLab = document.createElement("label");
    textLab.textContent = "format";
    const textIn = document.createElement("input");
    textIn.type = "text";
    textIn.value = el.text || "{current} / {total}";
    textIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.text = textIn.value || "{current} / {total}";
      renderAll();
    });
    textLab.appendChild(textIn);
    insp.appendChild(textLab);

    const alignLab = document.createElement("label");
    alignLab.textContent = "align";
    const alignSel = document.createElement("select");
    ["left", "center", "right"].forEach((v) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = v;
      if (String(el.align || "center") === v) o.selected = true;
      alignSel.appendChild(o);
    });
    alignSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.align = alignSel.value;
      renderAll();
    });
    alignLab.appendChild(alignSel);
    insp.appendChild(alignLab);
  }

  function renderBoxTextInspector(insp, el) {
    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent = "Box text — hardcode / bind เดียว หรือ items[] หลาย value ในแถว";
    insp.appendChild(tip);

    const textLab = document.createElement("label");
    textLab.textContent = "text (hardcode, ถ้าไม่มี items)";
    const textIn = document.createElement("input");
    textIn.type = "text";
    textIn.value = el.text || "";
    textIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.text = textIn.value;
      renderAll();
    });
    textLab.appendChild(textIn);
    insp.appendChild(textLab);

    const bindLab = document.createElement("label");
    bindLab.textContent = "bind (optional)";
    const bindIn = document.createElement("input");
    bindIn.type = "text";
    bindIn.value = el.bind || "";
    bindIn.placeholder = "$.coPayCriteria.title";
    bindIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.bind = bindIn.value.trim() || undefined;
      renderAll();
    });
    bindLab.appendChild(bindIn);
    insp.appendChild(bindLab);

    const alignLab = document.createElement("label");
    alignLab.textContent = "align (single)";
    const alignSel = document.createElement("select");
    ["left", "center", "right"].forEach((a) => {
      const o = document.createElement("option");
      o.value = a;
      o.textContent = a;
      if ((el.align || "center") === a) o.selected = true;
      alignSel.appendChild(o);
    });
    alignSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.align = alignSel.value;
      renderAll();
    });
    alignLab.appendChild(alignSel);
    insp.appendChild(alignLab);

    const fsLab = document.createElement("label");
    fsLab.textContent = "fontSize";
    const fsIn = document.createElement("input");
    fsIn.type = "number";
    fsIn.step = "0.5";
    fsIn.value = String((el.chrome && el.chrome.fontSize) || 7.5);
    fsIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.chrome = el.chrome || {};
      el.chrome.fontSize = Number(fsIn.value) || 7.5;
      renderAll();
    });
    fsLab.appendChild(fsIn);
    insp.appendChild(fsLab);

    const itemsHead = document.createElement("p");
    itemsHead.innerHTML = "<strong>Items (multi-value)</strong>";
    insp.appendChild(itemsHead);

    if (!Array.isArray(el.items)) el.items = [];

    el.items.forEach((item, idx) => {
      const box = document.createElement("div");
      box.className = "box-text-item-edit";

      const row1 = document.createElement("div");
      row1.className = "col-row";
      [["label", "label"], ["bind", "bind"], ["align", "align"]].forEach(([key, ph]) => {
        if (key === "align") {
          const sel = document.createElement("select");
          ["left", "center", "right"].forEach((a) => {
            const o = document.createElement("option");
            o.value = a;
            o.textContent = a;
            if (String(item.align || "left") === a) o.selected = true;
            sel.appendChild(o);
          });
          sel.title = "align";
          sel.addEventListener("change", () => {
            if (canvasTools) canvasTools.pushHistory();
            item.align = sel.value;
            renderAll();
          });
          row1.appendChild(sel);
          return;
        }
        const inp = document.createElement("input");
        inp.type = "text";
        inp.placeholder = ph;
        inp.value = item[key] || "";
        inp.addEventListener("change", () => {
          if (canvasTools) canvasTools.pushHistory();
          item[key] = inp.value.trim() || undefined;
          renderAll();
        });
        row1.appendChild(inp);
      });
      box.appendChild(row1);

      const row2 = document.createElement("div");
      row2.className = "col-row";
      [["label2", "label2"], ["bind2", "bind2"], ["flex", "flex"]].forEach(([key, ph]) => {
        const inp = document.createElement("input");
        inp.type = key === "flex" ? "number" : "text";
        if (key === "flex") inp.step = "0.1";
        inp.placeholder = ph;
        inp.value = item[key] != null ? String(item[key]) : "";
        inp.addEventListener("change", () => {
          if (canvasTools) canvasTools.pushHistory();
          if (key === "flex") {
            const n = Number(inp.value);
            item.flex = Number.isFinite(n) && n > 0 ? n : undefined;
          } else {
            item[key] = inp.value.trim() || undefined;
          }
          renderAll();
        });
        row2.appendChild(inp);
      });
      box.appendChild(row2);

      const rm = document.createElement("button");
      rm.type = "button";
      rm.className = "ghost";
      rm.textContent = "Remove item " + (idx + 1);
      rm.addEventListener("click", () => {
        if (canvasTools) canvasTools.pushHistory();
        el.items.splice(idx, 1);
        if (!el.items.length) delete el.items;
        renderAll();
      });
      box.appendChild(rm);
      insp.appendChild(box);
    });

    const addItem = document.createElement("button");
    addItem.type = "button";
    addItem.textContent = "+ Item";
    addItem.addEventListener("click", () => {
      if (canvasTools) canvasTools.pushHistory();
      if (!Array.isArray(el.items)) el.items = [];
      el.items.push({ label: "", bind: "", align: "left", flex: 1 });
      renderAll();
    });
    insp.appendChild(addItem);
  }

  function renderHeaderInspector(insp, el) {
    const preset = resolveHeaderPreset(el) || {
      columns: [],
      metaLines: [],
      bottomFields: [],
      showHdPerWeek: true,
    };
    const working = ensureWorkingHeaderPreset(el, preset);

    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent = "ลากเส้นแบ่งบน canvas · แก้ field / bind ด้านล่าง";
    insp.appendChild(tip);

    const modeLab = document.createElement("label");
    modeLab.textContent = "Bottom mode";
    const modeSel = document.createElement("select");
    [
      ["diagnosis", "diagnosis (Diagnosis / Allergy / HD)"],
      ["checklist-patient", "checklist-patient (DOB / sessions / days / mode / underlying)"],
      ["none", "none (hide bottom)"],
    ].forEach(([v, t]) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = t;
      const cur = String(el.bottomMode || working.bottomMode || "diagnosis").toLowerCase();
      if (cur === v) o.selected = true;
      modeSel.appendChild(o);
    });
    modeSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.bottomMode = modeSel.value;
      // Keep box height in sync with selected profile height when known
      const sets = working.bottomFieldSets || (preset && preset.bottomFieldSets) || {};
      const set = sets[el.bottomMode];
      const titleH = Number(working.titleRowHeightMm || 21.6);
      const bottomH = el.bottomMode === "none"
        ? 0
        : (set && Number(set.heightMm) > 0
          ? Number(set.heightMm)
          : Number(working.bottomRowHeightMm || 5.4));
      el.box = Object.assign({}, el.box, { hMm: titleH + bottomH });
      renderAll();
    });
    modeLab.appendChild(modeSel);
    insp.appendChild(modeLab);

    const tog = document.createElement("label");
    tog.className = "check-lab";
    const hdCb = document.createElement("input");
    hdCb.type = "checkbox";
    hdCb.checked = !!working.showHdPerWeek;
    hdCb.addEventListener("change", () => {
      working.showHdPerWeek = hdCb.checked;
      commitHeaderWorking(el, working);
      renderAll();
    });
    tog.appendChild(hdCb);
    tog.appendChild(document.createTextNode(" showHdPerWeek"));
    insp.appendChild(tog);

    const dateTog = document.createElement("label");
    dateTog.className = "check-lab";
    const dateCb = document.createElement("input");
    dateCb.type = "checkbox";
    dateCb.checked = !!working.showDateAndHdNo;
    dateCb.addEventListener("change", () => {
      working.showDateAndHdNo = dateCb.checked;
      commitHeaderWorking(el, working);
      renderAll();
    });
    dateTog.appendChild(dateCb);
    dateTog.appendChild(document.createTextNode(" showDateAndHdNo"));
    insp.appendChild(dateTog);

    const colHead = document.createElement("p");
    colHead.innerHTML = "<strong>Band columns</strong>";
    insp.appendChild(colHead);
    (working.columns || []).forEach((col, idx) => {
      const row = document.createElement("div");
      row.className = "col-row";
      const kind = document.createElement("input");
      kind.type = "text";
      kind.value = col.kind || "";
      kind.title = "kind: logo | title | meta";
      kind.addEventListener("change", () => {
        col.kind = kind.value.trim() || col.kind;
        commitHeaderWorking(el, working);
        renderAll();
      });
      const w = document.createElement("input");
      w.type = "number";
      w.step = "0.1";
      w.value = col.widthMm != null ? String(col.widthMm) : "";
      w.placeholder = "mm";
      w.title = "widthMm";
      w.addEventListener("change", () => {
        const n = Number(w.value);
        if (n > 0) { col.widthMm = n; col.weight = 1; }
        else { delete col.widthMm; }
        commitHeaderWorking(el, working);
        renderAll();
      });
      row.appendChild(kind);
      row.appendChild(w);
      insp.appendChild(row);
    });

    const metaHead = document.createElement("p");
    metaHead.innerHTML = "<strong>Meta lines</strong>";
    insp.appendChild(metaHead);
    (working.metaLines || []).forEach((line) => {
      const row = document.createElement("div");
      row.className = "col-row";
      const lab = document.createElement("input");
      lab.type = "text";
      lab.value = line.label || "";
      lab.addEventListener("change", () => {
        line.label = lab.value;
        commitHeaderWorking(el, working);
        renderAll();
      });
      const bind = document.createElement("input");
      bind.type = "text";
      bind.value = line.bind || "";
      bind.placeholder = "$.header.patient.name";
      bind.addEventListener("change", () => {
        line.bind = bind.value.trim();
        commitHeaderWorking(el, working);
        renderAll();
      });
      row.appendChild(lab);
      row.appendChild(bind);
      insp.appendChild(row);
    });

    const addMeta = document.createElement("button");
    addMeta.type = "button";
    addMeta.textContent = "+ Meta line";
    addMeta.addEventListener("click", () => {
      working.metaLines = working.metaLines || [];
      working.metaLines.push({ id: "m" + Math.random().toString(36).slice(2, 5), label: "Label", bind: "" });
      commitHeaderWorking(el, working);
      renderAll();
    });
    insp.appendChild(addMeta);

    const botHead = document.createElement("p");
    botHead.innerHTML = "<strong>Bottom fields</strong>";
    insp.appendChild(botHead);
    (working.bottomFields || []).forEach((line) => {
      const row = document.createElement("div");
      row.className = "col-row";
      const lab = document.createElement("input");
      lab.type = "text";
      lab.value = line.label || "";
      lab.addEventListener("change", () => {
        line.label = lab.value;
        commitHeaderWorking(el, working);
        renderAll();
      });
      const bind = document.createElement("input");
      bind.type = "text";
      bind.value = line.bind || "";
      bind.addEventListener("change", () => {
        line.bind = bind.value.trim();
        commitHeaderWorking(el, working);
        renderAll();
      });
      row.appendChild(lab);
      row.appendChild(bind);
      insp.appendChild(row);
    });

    const addBot = document.createElement("button");
    addBot.type = "button";
    addBot.textContent = "+ Bottom field";
    addBot.addEventListener("click", () => {
      working.bottomFields = working.bottomFields || [];
      working.bottomFields.push({ id: "b" + Math.random().toString(36).slice(2, 5), label: "Field", bind: "", weight: 1 });
      commitHeaderWorking(el, working);
      renderAll();
    });
    insp.appendChild(addBot);

    renderHeaderPresetActions(insp, el, working);
  }

  function renderHeaderPresetActions(insp, el, working) {
    if (el.preset && !el.headerPreset) {
      const detach = document.createElement("button");
      detach.type = "button";
      detach.textContent = "Detach preset (edit inline)";
      detach.addEventListener("click", () => {
        if (canvasTools) canvasTools.pushHistory();
        ensureWorkingHeaderPreset(el, resolveHeaderPreset(el) || working);
        renderAll();
      });
      insp.appendChild(detach);
    }

    const save = document.createElement("button");
    save.type = "button";
    save.textContent = "Save as preset";
    save.addEventListener("click", async () => {
      const id = prompt("Header preset id", working.id || el.preset || "my-header-preset");
      if (!id) return;
      const body = Object.assign({}, working, { id, displayName: working.displayName || id });
      await apiRef(`/api/hprp/presets/headers/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      headerPresets[id] = body;
      el.preset = id;
      delete el.headerPreset;
      setStatusRef("Saved header preset " + id, "ok");
      if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
        global.LibraryStudio.refresh();
      }
      renderAll();
    });
    insp.appendChild(save);

    const load = document.createElement("button");
    load.type = "button";
    load.textContent = "Load preset";
    load.addEventListener("click", () => {
      const ids = Object.keys(headerPresets);
      const id = prompt("Header preset id\n" + ids.join(", "), el.preset || ids[0] || "");
      if (!id || !headerPresets[id]) return;
      if (canvasTools) canvasTools.pushHistory();
      el.preset = id;
      delete el.headerPreset;
      renderAll();
    });
    insp.appendChild(load);
  }

  function renderDataGridInspector(insp, el) {
    const model = resolveDataGridModel(el, sampleData);
    const colCount = Math.max(1, (model.headers && model.headers.length) || 1);
    ensureDataGridColumnWidths(el, colCount);

    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent =
      "Lab data-grid — ลากเส้นคั่นคอลัมน์บน canvas หรือแก้ token ด้านล่าง (3 / * / 12mm / 1.5)";
    insp.appendChild(tip);

    [["bindRows", el.bindRows || ""], ["columnHeadersBind", el.columnHeadersBind || ""]].forEach(([label, val]) => {
      const lab = document.createElement("label");
      lab.textContent = label + " (read-only)";
      const inp = document.createElement("input");
      inp.type = "text";
      inp.readOnly = true;
      inp.value = val;
      lab.appendChild(inp);
      insp.appendChild(lab);
    });

    const syncBtn = document.createElement("button");
    syncBtn.type = "button";
    syncBtn.textContent = "Sync columnWidths จาก sample (" + colCount + " cols)";
    syncBtn.addEventListener("click", () => {
      if (canvasTools) canvasTools.pushHistory();
      ensureDataGridColumnWidths(el, colCount);
      renderAll();
      if (setStatusRef) setStatusRef("columnWidths sync แล้ว (" + colCount + " คอลัมน์)", "ok");
    });
    insp.appendChild(syncBtn);

    const widthsHead = document.createElement("p");
    widthsHead.innerHTML = "<strong>Column widths</strong>";
    insp.appendChild(widthsHead);

    if (!el.chrome) el.chrome = {};
    if (!Array.isArray(el.chrome.columnWidths)) el.chrome.columnWidths = ["3", "*"];
    while (el.chrome.columnWidths.length < colCount) {
      el.chrome.columnWidths.push(el.chrome.columnWidths.length === 0 ? "3" : "*");
    }

    for (let i = 0; i < colCount; i++) {
      const lab = document.createElement("label");
      const hdr = (model.headers && model.headers[i]) || ("Col " + (i + 1));
      lab.textContent = (i === 0 ? "Lab" : "Col " + i) + " · " + (hdr || "—");
      const inp = document.createElement("input");
      inp.type = "text";
      inp.placeholder = i === 0 ? "3 หรือ 12mm" : "* หรือ 1.5";
      inp.value = el.chrome.columnWidths[i] || (i === 0 ? "3" : "*");
      inp.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        el.chrome.columnWidths[i] = inp.value.trim() || (i === 0 ? "3" : "*");
        renderAll();
      });
      lab.appendChild(inp);
      insp.appendChild(lab);
    }

    const fsLab = document.createElement("label");
    fsLab.textContent = "fontSize";
    const fsIn = document.createElement("input");
    fsIn.type = "number";
    fsIn.step = "0.5";
    fsIn.value = String((el.chrome && el.chrome.fontSize) || 7);
    fsIn.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      el.chrome = el.chrome || {};
      el.chrome.fontSize = Number(fsIn.value) || 7;
      renderAll();
    });
    fsLab.appendChild(fsIn);
    insp.appendChild(fsLab);
  }

  function renderTableInspector(insp, el) {
    const preset = resolveTablePreset(el) || { rowMode: "annual", groupCount: 12, slotsPerGroup: 3, columns: [] };
    if (!el.tablePreset && el.presetId) {
      const detach = document.createElement("button");
      detach.type = "button";
      detach.textContent = "Detach preset (edit inline)";
      detach.addEventListener("click", () => {
        el.tablePreset = JSON.parse(JSON.stringify(preset));
        delete el.presetId;
        renderAll();
      });
      insp.appendChild(detach);
    }
    const working = ensureWorkingPreset(el, preset);

    const rowModeLabel = document.createElement("label");
    rowModeLabel.textContent = "Row mode";
    const rowModeSel = document.createElement("select");
    ["annual", "monthly", "freedom", "matrix"].forEach((m) => {
      const opt = document.createElement("option");
      opt.value = m;
      opt.textContent = m;
      if (working.rowMode === m) opt.selected = true;
      rowModeSel.appendChild(opt);
    });
    rowModeSel.addEventListener("change", () => {
      working.rowMode = rowModeSel.value;
      if (working.rowMode === "annual") working.groupCount = 12;
      commitWorking(el, working);
      renderAll();
    });
    rowModeLabel.appendChild(rowModeSel);
    insp.appendChild(rowModeLabel);

    if (String(working.rowMode || "").toLowerCase() === "matrix") {
      const tip = document.createElement("p");
      tip.className = "muted";
      tip.textContent =
        "Matrix 2 โซน (Item | แถบเดือน) — columnWidths รับ 46mm / * / 1.5 · ลากเส้นบน canvas · Library: progress-note-checklist-matrix-v1";
      insp.appendChild(tip);

      el.chrome = mergeTableChrome(el);
      const zones = readMatrixZoneTokens(el);

      function addZoneField(labelText, zoneKey) {
        const lab = document.createElement("label");
        lab.textContent = labelText;
        const inp = document.createElement("input");
        inp.type = "text";
        inp.placeholder = "46mm หรือ * หรือ 1.5";
        inp.value = formatMatrixWidthToken(zones[zoneKey]);
        inp.addEventListener("change", () => {
          if (canvasTools) canvasTools.pushHistory();
          const parsed = parseMixedColumnToken(inp.value);
          if (!parsed) {
            if (setStatusRef) setStatusRef("รูปแบบความกว้างไม่ถูกต้อง (ใช้ 46mm / * / 1.5)", "warn");
            inp.value = formatMatrixWidthToken(zones[zoneKey]);
            return;
          }
          zones[zoneKey] = parsed;
          writeMatrixZoneTokens(el, working, zones.item, zones.band);
          commitWorking(el, working);
          renderAll();
        });
        lab.appendChild(inp);
        insp.appendChild(lab);
      }

      addZoneField("Item width", "item");
      addZoneField("Month band width", "band");
    }

    if (String(working.rowMode || "").toLowerCase() === "freedom") {
      const freeLab = document.createElement("label");
      freeLab.textContent = "Freedom rows";
      const freeIn = document.createElement("input");
      freeIn.type = "number";
      freeIn.min = "1";
      freeIn.max = "40";
      freeIn.value = String(working.freedomRowCount || 2);
      freeIn.addEventListener("change", () => {
        working.freedomRowCount = Math.max(1, Number(freeIn.value) || 2);
        commitWorking(el, working);
        renderAll();
      });
      freeLab.appendChild(freeIn);
      insp.appendChild(freeLab);
    }

    const hasSoapProgress = (working.columns || []).some(
      (c) => String((c && c.cellKind) || "").toLowerCase() === "soap-progress");
    if (hasSoapProgress) {
      const tip = document.createElement("p");
      tip.className = "muted";
      tip.textContent =
        "SOAP progress — ลากเส้นคอลัมน์บน canvas · ลากเส้นแนวนอนในช่อง Progress เพื่อปรับ bandWeights S/O/A/P";
      insp.appendChild(tip);

      el.chrome = mergeTableChrome(el);
      const weights = soapBandWeights(el.chrome);
      const bandLab = document.createElement("label");
      bandLab.textContent = "bandWeights (S,O,A,P)";
      const bandIn = document.createElement("input");
      bandIn.type = "text";
      bandIn.value = weights.join(", ");
      bandIn.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        const parts = String(bandIn.value || "")
          .split(/[, ]+/)
          .map((x) => Number(x))
          .filter((n) => Number.isFinite(n) && n > 0);
        while (parts.length < 4) parts.push(1);
        el.chrome = mergeTableChrome(el);
        el.chrome.bandWeights = parts.slice(0, 4);
        if (working.chrome) working.chrome.bandWeights = el.chrome.bandWeights.slice();
        commitWorking(el, working);
        renderAll();
      });
      bandLab.appendChild(bandIn);
      insp.appendChild(bandLab);
    }

    const slotsLabel = document.createElement("label");
    slotsLabel.textContent = "Slots per group";
    const slotsInput = document.createElement("input");
    slotsInput.type = "number";
    slotsInput.min = "1";
    slotsInput.max = "10";
    slotsInput.value = String(working.slotsPerGroup || 3);
    slotsInput.addEventListener("change", () => {
      working.slotsPerGroup = Number(slotsInput.value) || 3;
      commitWorking(el, working);
      renderAll();
    });
    slotsLabel.appendChild(slotsInput);
    insp.appendChild(slotsLabel);

    const colHead = document.createElement("p");
    colHead.innerHTML = "<strong>Columns</strong> <span class=\"muted\">ลากเส้นคอลัมน์บน canvas ได้</span>";
    insp.appendChild(colHead);

    (working.columns || []).forEach((col, idx) => {
      const row = document.createElement("div");
      row.className = "col-row";
      const name = document.createElement("input");
      name.type = "text";
      name.title = "ชื่อที่แสดงบนหัวตาราง (title)";
      name.placeholder = "title";
      name.value = col.title || col.labelKey || col.id || "";
      name.addEventListener("change", () => {
        const v = name.value.trim() || col.id;
        col.title = v;
        if (!col.labelKey || col.labelKey === col.id) col.labelKey = v;
        commitWorking(el, working);
        renderAll();
      });
      const idInp = document.createElement("input");
      idInp.type = "text";
      idInp.className = "col-id";
      idInp.title = "column id (binding key)";
      idInp.placeholder = "id";
      idInp.value = col.id || "";
      idInp.addEventListener("change", () => {
        const nextId = idInp.value.trim() || col.id;
        const prevId = col.id;
        col.id = nextId;
        if (col.labelKey === prevId) col.labelKey = nextId;
        if ((col.title || "") === prevId) col.title = nextId;
        // Keep bindings in sync when id changes
        (el.bindings || []).forEach((b) => {
          if (b.column === prevId) b.column = nextId;
        });
        commitWorking(el, working);
        renderAll();
      });
      const kindSel = document.createElement("select");
      kindSel.className = "col-kind";
      kindSel.title = "cellKind";
      [["text", "text"], ["soap-progress", "soap"]].forEach(([v, t]) => {
        const o = document.createElement("option");
        o.value = v;
        o.textContent = t;
        if (String(col.cellKind || "text").toLowerCase() === v) o.selected = true;
        kindSel.appendChild(o);
      });
      kindSel.addEventListener("change", () => {
        col.cellKind = kindSel.value === "text" ? "text" : kindSel.value;
        commitWorking(el, working);
        renderAll();
      });
      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "−";
      del.addEventListener("click", () => {
        working.columns.splice(idx, 1);
        commitWorking(el, working);
        renderAll();
      });
      row.appendChild(name);
      row.appendChild(idInp);
      row.appendChild(kindSel);
      row.appendChild(del);
      insp.appendChild(row);
    });

    const addCol = document.createElement("button");
    addCol.type = "button";
    addCol.textContent = "+ Column";
    addCol.addEventListener("click", () => {
      const id = "col_" + Math.random().toString(36).slice(2, 6);
      working.columns = working.columns || [];
      working.columns.push({ id, labelKey: id, title: id, weight: 1, center: false, isLab: false, cellKind: "text" });
      commitWorking(el, working);
      renderAll();
    });
    insp.appendChild(addCol);

    renderBindings(insp, el);
    renderPresetActions(insp, el, working);
  }

  function ensureWorkingPreset(el, preset) {
    if (el.tablePreset) return el.tablePreset;
    el.tablePreset = JSON.parse(JSON.stringify(preset));
    if (!el.tablePreset.id) el.tablePreset.id = el.presetId || "inline-table";
    delete el.presetId;
    return el.tablePreset;
  }

  function commitWorking(el, working) {
    // Persist inline preset so Download PDF uses the same weights as canvas
    if (el.chrome && el.chrome.columnWidths) {
      working.chrome = Object.assign({}, working.chrome || {}, {
        columnWidths: el.chrome.columnWidths.slice(),
      });
    }
    el.tablePreset = working;
    delete el.presetId;
    el.columnOverrides = (working.columns || []).map((c) => ({
      id: c.id,
      labelKey: c.labelKey,
      title: c.title,
      weight: Number(c.weight) || 1,
      center: !!c.center,
      isLab: !!c.isLab,
    }));
    if (working.dateColumns) {
      el.tablePreset.dateColumns = {
        monthWeight: Number(working.dateColumns.monthWeight) || 0.45,
        dayWeight: Number(working.dateColumns.dayWeight) || 1.35,
        dateHeaderLabelKey: working.dateColumns.dateHeaderLabelKey || "colDate",
      };
    }
    stateRef.draft.manifest.layoutMode = "designer";
  }

  function renderPageInspector(insp) {
    ensureElements();
    const page = stateRef.draft.layout.page;
    const tip = document.createElement("p");
    tip.className = "muted";
    tip.textContent = "ขอบหน้า (margin) · ช่องว่างระหว่าง block · เส้นกรอบหน้า";
    insp.appendChild(tip);

    const mLab = document.createElement("label");
    mLab.textContent = "marginMm (รอบด้าน)";
    const mi = document.createElement("input");
    mi.type = "number";
    mi.min = "0";
    mi.max = "40";
    mi.step = "0.5";
    mi.value = String(page.marginMm != null ? page.marginMm : 2);
    mi.addEventListener("change", () => {
      page.marginMm = Number(mi.value);
      page.margin = undefined;
      reflowElements();
      renderAll();
    });
    mLab.appendChild(mi);
    insp.appendChild(mLab);

    ["top", "right", "bottom", "left"].forEach((side) => {
      const lab = document.createElement("label");
      lab.textContent = "margin." + side;
      const input = document.createElement("input");
      input.type = "number";
      input.min = "0";
      input.step = "0.5";
      const cur = (page.margin && page.margin[side] != null)
        ? page.margin[side]
        : (page.marginMm != null ? page.marginMm : 2);
      input.value = String(cur);
      input.addEventListener("change", () => {
        page.margin = page.margin || {};
        page.margin[side] = Number(input.value);
        reflowElements();
        renderAll();
      });
      lab.appendChild(input);
      insp.appendChild(lab);
    });

    const modeLab = document.createElement("label");
    modeLab.textContent = "spacing ระหว่าง block";
    const modeSel = document.createElement("select");
    [
      ["custom", "กำหนดเอง (custom)"],
      ["margin", "เท่า margin ขอบ"],
      ["none", "ไม่เว้น (ขอบติดกัน)"],
    ].forEach(([v, t]) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = t;
      if (String(page.spacingMode || "custom").toLowerCase() === v) o.selected = true;
      modeSel.appendChild(o);
    });
    modeSel.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      page.spacingMode = modeSel.value;
      reflowElements();
      renderAll();
    });
    modeLab.appendChild(modeSel);
    insp.appendChild(modeLab);

    const modeHint = document.createElement("p");
    modeHint.className = "muted";
    const modeNow = String(page.spacingMode || "custom").toLowerCase();
    if (modeNow === "none") {
      modeHint.textContent = "ชิดกันและทับขอบเล็กน้อยให้เส้นดูเป็นเส้นเดียว (canvas ↔ PDF)";
    } else if (modeNow === "margin") {
      modeHint.textContent = "ใช้ค่า marginMm เดียวกันทั้งข้างและล่าง";
    } else {
      modeHint.textContent =
        "ตั้ง below / beside แยกได้ · 0 = ชิดขอบ (collapse) · ลอง 0.2–0.5 ถ้าต้องการช่องว่างบาง ๆ";
    }
    insp.appendChild(modeHint);

    if (modeNow === "custom") {
      const sharedLab = document.createElement("label");
      sharedLab.textContent = "spacingMm (ทั้งคู่ ถ้าไม่แยก)";
      const sharedIn = document.createElement("input");
      sharedIn.type = "number";
      sharedIn.min = "0";
      sharedIn.step = "0.1";
      sharedIn.value = String(page.spacingMm != null ? page.spacingMm : 2);
      sharedIn.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        page.spacingMm = Number(sharedIn.value);
        reflowElements();
        renderAll();
      });
      sharedLab.appendChild(sharedIn);
      insp.appendChild(sharedLab);

      const belowLab = document.createElement("label");
      belowLab.textContent = "spacingBelowMm (block ล่าง)";
      const belowIn = document.createElement("input");
      belowIn.type = "number";
      belowIn.min = "0";
      belowIn.step = "0.1";
      belowIn.placeholder = "ใช้ spacingMm";
      belowIn.value = page.spacingBelowMm != null ? String(page.spacingBelowMm) : "";
      belowIn.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        if (belowIn.value === "") delete page.spacingBelowMm;
        else page.spacingBelowMm = Number(belowIn.value);
        reflowElements();
        renderAll();
      });
      belowLab.appendChild(belowIn);
      insp.appendChild(belowLab);

      const besideLab = document.createElement("label");
      besideLab.textContent = "spacingBesideMm (block ข้าง)";
      const besideIn = document.createElement("input");
      besideIn.type = "number";
      besideIn.min = "0";
      besideIn.step = "0.1";
      besideIn.placeholder = "ใช้ spacingMm";
      besideIn.value = page.spacingBesideMm != null ? String(page.spacingBesideMm) : "";
      besideIn.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        if (besideIn.value === "") delete page.spacingBesideMm;
        else page.spacingBesideMm = Number(besideIn.value);
        reflowElements();
        renderAll();
      });
      besideLab.appendChild(besideIn);
      insp.appendChild(besideLab);
    }

    const borderLab = document.createElement("label");
    borderLab.className = "check-lab";
    const borderCb = document.createElement("input");
    borderCb.type = "checkbox";
    borderCb.checked = String(page.border || "none").toLowerCase() === "thin";
    borderCb.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      page.border = borderCb.checked ? "thin" : "none";
      renderAll();
    });
    borderLab.appendChild(borderCb);
    borderLab.appendChild(document.createTextNode(" เส้นขอบหน้า (page border)"));
    insp.appendChild(borderLab);

    const orient = document.createElement("label");
    orient.textContent = "orientation";
    const os = document.createElement("select");
    ["portrait", "landscape"].forEach((v) => {
      const o = document.createElement("option");
      o.value = v;
      o.textContent = v;
      if (String(page.orientation || "portrait") === v) o.selected = true;
      os.appendChild(o);
    });
    os.addEventListener("change", () => {
      if (canvasTools) canvasTools.pushHistory();
      page.orientation = os.value;
      reflowElements();
      renderAll();
    });
    orient.appendChild(os);
    insp.appendChild(orient);
  }

  function renderBindings(insp, el) {
    const head = document.createElement("p");
    head.innerHTML = "<strong>Field mapping</strong>";
    insp.appendChild(head);
    (el.bindings || []).forEach((b, i) => {
      const row = document.createElement("div");
      row.className = "bind-row";
      row.innerHTML = `<span class="muted">${escapeHtml(b.column)} ← ${escapeHtml(b.path)}</span>`;
      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "×";
      del.addEventListener("click", () => {
        el.bindings.splice(i, 1);
        renderAll();
      });
      row.appendChild(del);
      insp.appendChild(row);
    });

    if (adapterSchema && adapterSchema.fields) {
      const pick = document.createElement("select");
      pick.innerHTML = "<option value=\"\">Map field…</option>";
      flattenFields(adapterSchema.fields).forEach((f) => {
        const opt = document.createElement("option");
        opt.value = JSON.stringify(f);
        opt.textContent = f.path;
        pick.appendChild(opt);
      });
      pick.addEventListener("change", () => {
        if (!pick.value) return;
        const f = JSON.parse(pick.value);
        el.bindings = el.bindings || [];
        el.bindings.push({
          path: f.path,
          column: guessColumn(f.path),
          context: f.path.includes("monthLabel") ? "group-label" : "entry",
        });
        pick.value = "";
        renderAll();
      });
      insp.appendChild(pick);
    }
  }

  function flattenFields(fields, out) {
    out = out || [];
    (fields || []).forEach((f) => {
      if (f.path) out.push(f);
      if (f.children) flattenFields(f.children, out);
    });
    return out;
  }

  function guessColumn(path) {
    if (path.includes("monthLabel")) return "month";
    if (path.includes("dayLabel")) return "day";
    if (path.includes("labIsHistorical")) return "lab";
    return path.split(".").pop().replace("[]", "");
  }

  function renderPresetActions(insp, el, working) {
    const save = document.createElement("button");
    save.type = "button";
    save.textContent = "Save as preset";
    save.addEventListener("click", async () => {
      const id = prompt("Preset id", working.id || "my-table-preset");
      if (!id) return;
      const body = Object.assign({}, working, { id, displayName: working.displayName || id });
      await apiRef(`/api/hprp/presets/tables/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      tablePresets[id] = body;
      el.presetId = id;
      el.tablePreset = undefined;
      setStatusRef("Saved preset " + id, "ok");
      renderAll();
    });
    insp.appendChild(save);

    const load = document.createElement("button");
    load.type = "button";
    load.textContent = "Load preset";
    load.addEventListener("click", () => {
      const ids = Object.keys(tablePresets);
      const id = prompt("Preset id\n" + ids.join(", "), el.presetId || ids[0] || "");
      if (!id || !tablePresets[id]) return;
      el.presetId = id;
      el.tablePreset = undefined;
      renderAll();
    });
    insp.appendChild(load);
  }

  function renderBoxFields(insp, el) {
    const note = document.createElement("p");
    note.className = "muted";
    note.textContent = "ขนาด (mm) — ลากขอบบน canvas หรือแก้ตรงนี้";
    insp.appendChild(note);
    ["xMm", "yMm", "wMm", "hMm"].forEach((key) => {
      const lab = document.createElement("label");
      lab.textContent = "box." + key;
      const input = document.createElement("input");
      input.type = "number";
      input.step = "0.5";
      if (key === "hMm") input.min = String(minHeightForElement(el));
      if (key === "wMm") input.min = String(MIN_BLOCK_W);
      input.value = String((el.box && el.box[key]) || 0);
      input.addEventListener("change", () => {
        if (canvasTools) canvasTools.pushHistory();
        el.box = el.box || {};
        let v = Number(input.value);
        if (key === "hMm") v = Math.max(minHeightForElement(el), v);
        if (key === "wMm") {
          v = Math.max(MIN_BLOCK_W, v);
          const m = pageMetrics();
          v = Math.min(maxWidthInRow(el, m.contentW, m.margins, m.gaps, m.scale), v);
        }
        el.box[key] = v;
        if (key === "wMm") el.manualWidth = true;
        reflowElements();
        renderAll();
      });
      lab.appendChild(input);
      insp.appendChild(lab);
    });
  }

  function addDataGrid() {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const m = pageMetrics();
    const id = "grid_" + Math.random().toString(36).slice(2, 7);
    const el = {
      id,
      type: "data-grid",
      band: "content",
      place: "below",
      bindRows: "$.rows",
      columnHeadersBind: "$.columnHeaders",
      box: { xMm: 0, yMm: 0, wMm: m.contentW, hMm: 120 },
      chrome: {
        border: "thin",
        fontSize: 7,
        headerFill: "$branding.sectionHeaderBackground",
        columnWidths: ["3", "*"],
      },
    };
    stateRef.draft.layout.elements.push(el);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
    setStatusRef("Inserted data-grid (lab matrix). Save pack to persist.", "ok");
  }

  function addConfigTable(opts) {
    const inner = !!(opts && opts.inner);
    ensureElements();
    promoteToDesignerIfNeeded();
    const id = "tbl_" + Math.random().toString(36).slice(2, 7);
    const el = {
      id,
      type: "config-table",
      band: "content",
      presetId: "hct-epo-annual-v1",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 100, hMm: 80 },
      bindings: [],
      chrome: { border: "thin" },
    };
    if (inner) {
      insertElementInnerBelow(el);
      return;
    }
    if (canvasTools) canvasTools.pushHistory();
    stateRef.draft.layout.elements.push(el);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
  }

  function addHeader(presetId) {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const ids = Object.keys(headerPresets);
    const pid = presetId || "clinical-header-thaiur";
    if (!headerPresets[pid] && ids.length && !presetId) {
      // fall through — still reference id; catalog may load later
    }
    const id = "hdr_" + Math.random().toString(36).slice(2, 7);
    const el = {
      id,
      type: "header",
      band: "header",
      preset: headerPresets[pid] ? pid : (ids[0] || pid),
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 206, hMm: 27 },
    };
    const els = stateRef.draft.layout.elements;
    let insertAt = 0;
    for (let i = 0; i < els.length; i++) {
      if (resolveBand(els[i]) === "header" || resolveBand(els[i]) === "super-header") {
        insertAt = i + 1;
      }
    }
    els.splice(insertAt, 0, el);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
    setStatusRef("Inserted header (" + el.preset + "). Save pack to persist.", "ok");
  }

  /**
   * Open a library header preset alone on the canvas for edit/save
   * (writes packages/library/headers/{id}.json — not a report .hprp).
   */
  async function openLibraryHeader(presetId) {
    await loadCatalogExtras({ silent: true });
    const pid = String(presetId || "").trim();
    let preset = headerPresets[pid];
    if (!preset) {
      try {
        preset = await apiRef(`/api/hprp/presets/headers/${encodeURIComponent(pid)}`);
        if (preset && preset.id) headerPresets[preset.id] = preset;
      } catch (_) {
        preset = null;
      }
    }
    if (!preset) return null;

    const working = JSON.parse(JSON.stringify(preset));
    const id = working.id || pid;
    working.id = id;
    const hMm = Number(working.titleRowHeightMm || 21.6) + Number(working.bottomRowHeightMm || 5.4);

    if (!stateRef) {
      console.error("[TableDesigner.openLibraryHeader] stateRef missing — init not called");
      return null;
    }

    stateRef.draft = {
      manifest: {
        id: "__library__/headers/" + id,
        displayName: working.displayName || id,
        layoutMode: "designer",
        layoutKind: "LibraryHeader",
      },
      layout: {
        page: { size: "A4", marginMm: 2, spacingMm: 2, border: "none" },
        elements: [
          {
            id: "hdr_lib",
            type: "header",
            band: "header",
            preset: id,
            headerPreset: working,
            place: "below",
            box: { xMm: 0, yMm: 0, wMm: 206, hMm: hMm > 0 ? hMm : 27 },
          },
        ],
        body: [],
      },
      labels: {},
    };
    selectedElementId = "hdr_lib";
    selectedElementIds = new Set(["hdr_lib"]);
    stateRef.selectedKey = null;

    try {
      sampleData = await apiRef("/api/hprp/packages/clinical-01-hct-epo/sample-data");
    } catch (_) {
      sampleData = null;
    }

    if (canvasTools) canvasTools.resetHistory();
    return { id: id, displayName: working.displayName || id };
  }

  async function saveLibraryHeader() {
    ensureElements();
    const el = (stateRef.draft.layout.elements || []).find((e) => e.type === "header");
    if (!el) throw new Error("No header element on canvas");
    const working = resolveHeaderPreset(el);
    if (!working) throw new Error("Header preset missing");
    ensureWorkingHeaderPreset(el, working);
    commitHeaderWorking(el, el.headerPreset);

    const id = (stateRef.libraryEdit && stateRef.libraryEdit.id)
      || el.preset
      || (el.headerPreset && el.headerPreset.id)
      || working.id;
    if (!id) throw new Error("Header preset id missing");

    const body = Object.assign({}, JSON.parse(JSON.stringify(el.headerPreset || working)), {
      id: id,
      displayName: (el.headerPreset && el.headerPreset.displayName)
        || working.displayName
        || (stateRef.libraryEdit && stateRef.libraryEdit.displayName)
        || id,
    });

    const result = await apiRef(`/api/hprp/presets/headers/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
    const saved = (result && result.preset) || body;
    headerPresets[id] = saved;
    el.preset = id;
    el.headerPreset = JSON.parse(JSON.stringify(saved));
    renderAll();
    return result || {
      preset: saved,
      outputPath: "packages/library/headers/" + id + ".json",
    };
  }

  async function deleteLibraryHeader(presetId) {
    const id = String(presetId || "").trim();
    if (!id) throw new Error("Header id required");
    if (!apiRef) throw new Error("API not ready");
    const result = await apiRef(`/api/hprp/presets/headers/${encodeURIComponent(id)}`, {
      method: "DELETE",
    });
    delete headerPresets[id];
    // If seed remains, reload catalog so seed reappears.
    await loadCatalogExtras({ silent: true });
    if (stateRef && stateRef.libraryEdit && stateRef.libraryEdit.id === id) {
      stateRef.libraryEdit = null;
    }
    if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
      global.LibraryStudio.refresh();
    }
    return result;
  }

  async function openLibraryTable(presetId) {
    await loadCatalogExtras({ silent: true });
    const pid = String(presetId || "").trim();
    let preset = tablePresets[pid];
    if (!preset) {
      try {
        preset = await apiRef(`/api/hprp/presets/tables/${encodeURIComponent(pid)}`);
        if (preset && preset.id) tablePresets[preset.id] = preset;
      } catch (_) {
        preset = null;
      }
    }
    if (!preset) return null;

    const working = JSON.parse(JSON.stringify(preset));
    const id = working.id || pid;
    working.id = id;
    if (!stateRef) {
      console.error("[TableDesigner.openLibraryTable] stateRef missing — init not called");
      return null;
    }

    stateRef.draft = {
      manifest: {
        id: "__library__/tables/" + id,
        displayName: working.displayName || id,
        layoutMode: "designer",
        layoutKind: "LibraryTable",
      },
      layout: {
        page: { size: "A4", marginMm: 2, spacingMm: 2, border: "none" },
        elements: [
          {
            id: "tbl_lib",
            type: "config-table",
            band: "content",
            presetId: id,
            tablePreset: working,
            place: "below",
            box: { xMm: 0, yMm: 0, wMm: 206, hMm: 140 },
            bindings: [],
            chrome: Object.assign(
              { border: "thin" },
              working.chrome && working.chrome.columnWidths
                ? { columnWidths: working.chrome.columnWidths.slice() }
                : {}),
          },
        ],
        body: [],
      },
      labels: {},
    };
    selectedElementId = "tbl_lib";
    selectedElementIds = new Set(["tbl_lib"]);
    stateRef.selectedKey = null;

    const isMatrix =
      String(working.rowMode || "").toLowerCase() === "matrix"
      || id.indexOf("progress-note-checklist-matrix") === 0;
    try {
      sampleData = await apiRef(
        isMatrix
          ? "/api/hprp/packages/clinical-05-progress-note-checklist/sample-data"
          : "/api/hprp/packages/clinical-01-hct-epo/sample-data");
    } catch (_) {
      sampleData = null;
    }
    if (isMatrix && sampleData) {
      sampleData = normalizeSampleForThaiUrHeader(sampleData);
    }

    if (canvasTools) canvasTools.resetHistory();
    return { id: id, displayName: working.displayName || id };
  }

  async function saveLibraryTable() {
    ensureElements();
    const el = (stateRef.draft.layout.elements || []).find((e) => e.type === "config-table");
    if (!el) throw new Error("No config-table on canvas");
    const working = resolveTablePreset(el);
    if (!working) throw new Error("Table preset missing");
    ensureWorkingPreset(el, working);
    commitWorking(el, el.tablePreset);

    const id = (stateRef.libraryEdit && stateRef.libraryEdit.id)
      || el.presetId
      || (el.tablePreset && el.tablePreset.id)
      || working.id;
    if (!id) throw new Error("Table preset id missing");

    const body = Object.assign({}, JSON.parse(JSON.stringify(el.tablePreset || working)), {
      id: id,
      displayName: (el.tablePreset && el.tablePreset.displayName)
        || working.displayName
        || (stateRef.libraryEdit && stateRef.libraryEdit.displayName)
        || id,
    });

    const result = await apiRef(`/api/hprp/presets/tables/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
    const saved = (result && result.preset) || body;
    tablePresets[id] = saved;
    el.presetId = id;
    el.tablePreset = JSON.parse(JSON.stringify(saved));
    renderAll();
    return result || {
      preset: saved,
      outputPath: "packages/library/tables/" + id + ".json",
    };
  }

  async function deleteLibraryTable(presetId) {
    const id = String(presetId || "").trim();
    if (!id) throw new Error("Table id required");
    if (!apiRef) throw new Error("API not ready");
    const result = await apiRef(`/api/hprp/presets/tables/${encodeURIComponent(id)}`, {
      method: "DELETE",
    });
    delete tablePresets[id];
    await loadCatalogExtras({ silent: true });
    if (stateRef && stateRef.libraryEdit && stateRef.libraryEdit.id === id) {
      stateRef.libraryEdit = null;
    }
    if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
      global.LibraryStudio.refresh();
    }
    return result;
  }

  async function openLibraryFragment(presetId) {
    await loadCatalogExtras({ silent: true });
    const pid = String(presetId || "").trim();
    let frag = fragmentPresets[pid];
    if (!frag) {
      try {
        frag = await apiRef(`/api/hprp/presets/fragments/${encodeURIComponent(pid)}`);
        if (frag && frag.id) fragmentPresets[frag.id] = frag;
      } catch (_) {
        frag = null;
      }
    }
    if (!frag || !Array.isArray(frag.elements) || !frag.elements.length) return null;

    const id = frag.id || pid;
    if (!stateRef) {
      console.error("[TableDesigner.openLibraryFragment] stateRef missing — init not called");
      return null;
    }

    const elements = JSON.parse(JSON.stringify(frag.elements));
    stateRef.draft = {
      manifest: {
        id: "__library__/fragments/" + id,
        displayName: frag.displayName || id,
        layoutMode: "designer",
        layoutKind: "LibraryFragment",
      },
      layout: {
        page: { size: "A4", marginMm: 2, spacingMm: 2, border: "none" },
        elements: elements,
        body: [],
      },
      labels: {},
    };
    const firstId = elements[0] && elements[0].id ? elements[0].id : null;
    setSingleSelection(firstId);
    stateRef.selectedKey = null;

    try {
      sampleData = await apiRef("/api/hprp/packages/clinical-01-hct-epo/sample-data");
    } catch (_) {
      sampleData = null;
    }

    if (canvasTools) canvasTools.resetHistory();
    return { id: id, displayName: frag.displayName || id };
  }

  async function saveLibraryFragment() {
    ensureElements();
    const id = (stateRef.libraryEdit && stateRef.libraryEdit.id) || null;
    if (!id) throw new Error("Fragment id missing");
    const els = stateRef.draft.layout.elements || [];
    if (!els.length) throw new Error("No elements on canvas");

    const body = {
      id: id,
      displayName: (stateRef.libraryEdit && stateRef.libraryEdit.displayName)
        || (fragmentPresets[id] && fragmentPresets[id].displayName)
        || id,
      tags: (fragmentPresets[id] && fragmentPresets[id].tags) || [],
      elements: JSON.parse(JSON.stringify(els)),
    };

    const result = await apiRef(`/api/hprp/presets/fragments/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
    const saved = (result && result.preset) || body;
    fragmentPresets[id] = saved;
    renderAll();
    return result || {
      preset: saved,
      outputPath: "packages/library/fragments/" + id + ".json",
    };
  }

  async function deleteLibraryFragment(presetId) {
    const id = String(presetId || "").trim();
    if (!id) throw new Error("Fragment id required");
    if (!apiRef) throw new Error("API not ready");
    const result = await apiRef(`/api/hprp/presets/fragments/${encodeURIComponent(id)}`, {
      method: "DELETE",
    });
    delete fragmentPresets[id];
    await loadCatalogExtras({ silent: true });
    if (stateRef && stateRef.libraryEdit && stateRef.libraryEdit.id === id) {
      stateRef.libraryEdit = null;
    }
    if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
      global.LibraryStudio.refresh();
    }
    return result;
  }

  function uniqueElementId(base, taken) {
    let id = base || "el";
    let n = 0;
    while (taken.has(id)) {
      n += 1;
      id = base + "_" + n;
    }
    taken.add(id);
    return id;
  }

  function insertFragment(fragmentId) {
    ensureElements();
    promoteToDesignerIfNeeded();
    const frag = fragmentPresets[fragmentId];
    if (!frag || !Array.isArray(frag.elements) || !frag.elements.length) {
      setStatusRef("Fragment not found: " + fragmentId, "err");
      return;
    }
    if (canvasTools) canvasTools.pushHistory();
    const taken = new Set(stateRef.draft.layout.elements.map((e) => e.id));
    const clones = frag.elements.map((src) => {
      const c = JSON.parse(JSON.stringify(src));
      c.id = uniqueElementId(c.id || c.type || "frag", taken);
      return c;
    });
    const els = stateRef.draft.layout.elements;
    let insertAt = els.length;
    if (selectedElementId) {
      const idx = els.findIndex((e) => e.id === selectedElementId);
      if (idx >= 0) insertAt = idx + 1;
    }
    els.splice(insertAt, 0, ...clones);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(clones[0].id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
    setStatusRef("Inserted fragment " + fragmentId + " (" + clones.length + " elements)", "ok");
  }

  function addFragmentPrompt() {
    const ids = Object.keys(fragmentPresets);
    if (!ids.length) {
      setStatusRef("No fragments in catalog yet", "err");
      return;
    }
    const id = prompt("Fragment id\n" + ids.join(", "), ids.indexOf("copay-duo-v1") >= 0 ? "copay-duo-v1" : ids[0]);
    if (!id) return;
    insertFragment(id);
  }

  function getCatalogSnapshot() {
    return {
      headers: headerPresets,
      tables: tablePresets,
      fragments: fragmentPresets,
    };
  }

  function getSelectedElement() {
    return findElementById(selectedElementId);
  }

  function collectElementsByIds(ids) {
    const want = new Set(ids || []);
    const out = [];
    function walk(list) {
      (list || []).forEach((e) => {
        if (isGroup(e)) walk(e.children);
        else if (want.has(e.id)) out.push(e);
      });
    }
    walk(stateRef.draft.layout.elements || []);
    return out;
  }

  function saveFragmentFromSelection(ids) {
    return (async () => {
      ensureElements();
      const want = Array.isArray(ids) && ids.length
        ? ids
        : getSelectedIdsInLayoutOrder();
      if (!want.length) {
        setStatusRef("Select element(s) first (Shift+click for multi)", "err");
        return;
      }
      const els = collectElementsByIds(want);
      if (!els.length) {
        setStatusRef("No matching selection", "err");
        return;
      }
      const id = prompt("Fragment id", "my-fragment-v1");
      if (!id) return;
      const displayName = prompt("Display name", id) || id;
      const body = {
        id,
        displayName,
        tags: [],
        elements: JSON.parse(JSON.stringify(els)),
      };
      const result = await apiRef(`/api/hprp/presets/fragments/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      const saved = (result && result.preset) || body;
      fragmentPresets[id] = saved;
      const path = (result && result.outputPath) || ("packages/library/fragments/" + id + ".json");
      setStatusRef("Saved fragment " + id + " → " + path, "ok");
      if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
        global.LibraryStudio.refresh();
      }
      return result;
    })();
  }

  /**
   * Add current canvas selection to Library:
   * - 1 header → header preset
   * - 1 config-table → table preset
   * - otherwise (1+ any / mixed / multi) → fragment
   */
  function addSelectionToLibrary() {
    return (async () => {
      ensureElements();
      const ids = getSelectedIdsInLayoutOrder();
      if (!ids.length) {
        setStatusRef("Select element(s) first — Shift+click to multi-select", "err");
        return;
      }
      const els = (stateRef.draft.layout.elements || []).filter((e) => ids.indexOf(e.id) >= 0);
      if (els.length === 1 && els[0].type === "header") {
        await saveHeaderPresetFromElement(els[0]);
        return;
      }
      if (els.length === 1 && els[0].type === "config-table") {
        await saveTablePresetFromElement(els[0]);
        return;
      }
      await saveFragmentFromSelection(ids);
    })();
  }

  function saveHeaderPresetFromElement(el) {
    return (async () => {
      if (!el || el.type !== "header") {
        setStatusRef("Select a header element", "err");
        return;
      }
      const working = resolveHeaderPreset(el) || { columns: [], metaLines: [], bottomFields: [] };
      const id = prompt("Header preset id", working.id || el.preset || "my-header-preset");
      if (!id) return;
      const body = Object.assign({}, JSON.parse(JSON.stringify(working)), {
        id,
        displayName: working.displayName || id,
      });
      await apiRef(`/api/hprp/presets/headers/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      headerPresets[id] = body;
      el.preset = id;
      delete el.headerPreset;
      setStatusRef("Saved header preset " + id, "ok");
      if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
        global.LibraryStudio.refresh();
      }
      renderAll();
    })();
  }

  function saveTablePresetFromElement(el) {
    return (async () => {
      if (!el || el.type !== "config-table") {
        setStatusRef("Select a table element", "err");
        return;
      }
      const working = resolveTablePreset(el) || { columns: [] };
      const id = prompt("Table preset id", working.id || el.presetId || "my-table-preset");
      if (!id) return;
      const body = Object.assign({}, JSON.parse(JSON.stringify(working)), {
        id,
        displayName: working.displayName || id,
      });
      await apiRef(`/api/hprp/presets/tables/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      tablePresets[id] = body;
      el.presetId = id;
      delete el.tablePreset;
      setStatusRef("Saved table preset " + id, "ok");
      if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
        global.LibraryStudio.refresh();
      }
      renderAll();
    })();
  }

  function addBoxText(opts) {
    const inner = !!(opts && opts.inner);
    ensureElements();
    promoteToDesignerIfNeeded();
    const id = "box_" + Math.random().toString(36).slice(2, 7);
    const el = {
      id,
      type: "box-text",
      band: "content",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: inner ? 100 : 206, hMm: 5 },
      text: "หัวข้อ",
      align: "center",
      chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground", fontSize: 7.5 },
    };
    if (inner) {
      insertElementInnerBelow(el);
      return;
    }
    if (canvasTools) canvasTools.pushHistory();
    stateRef.draft.layout.elements.push(el);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
  }

  function addNarrative() {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const m = pageMetrics();
    const id = "narr_" + Math.random().toString(36).slice(2, 7);
    const el = {
      id,
      type: "narrative",
      band: "content",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: m.contentW, hMm: 40 },
      paragraphs: [
        { text: "หัวข้อเอกสาร", role: "title", align: "center", sub: false },
        { text: "ย่อหน้าแรก — แก้ข้อความได้ใน Inspector", align: "left", sub: false },
      ],
      chrome: { border: "thin", fontSize: 11, rowHeightMm: 3.5 },
    };
    stateRef.draft.layout.elements.push(el);
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
    setStatusRef("Inserted narrative (Word-lite). Save pack to persist.", "ok");
  }

  function addPageOf() {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const id = "pageof_" + Math.random().toString(36).slice(2, 7);
    stateRef.draft.layout.elements.push({
      id,
      type: "page-of",
      band: "super-footer",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 206, hMm: 5 },
      text: "{current} / {total}",
      align: "center",
      chrome: { border: "none", fontSize: 8 },
    });
    stateRef.draft.manifest.layoutMode = "designer";
    setSingleSelection(id);
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
  }

  function syncBodyClass() {
    document.body.classList.toggle("mode-wysiwyg", isStudioCanvas());
    document.body.classList.toggle("mode-designer-layout", isStudioCanvas());
    document.body.classList.remove("mode-composition");
    const pane = document.getElementById("designerStudioPane");
    if (pane) pane.classList.remove("hidden");
  }

  function renderAll() {
    if (!isStudioCanvas()) return;
    promoteToDesignerIfNeeded();
    syncBodyClass();
    renderCanvas();
    renderInspector();
  }

  async function onPackageOpened() {
    clearElementSelection();
    stateRef.selectedKey = null;
    promoteToDesignerIfNeeded();
    await loadCatalogExtras();
    await loadSampleData();
    renderAll();
    if (canvasTools) canvasTools.resetHistory();
    if (setStatusRef) {
      setStatusRef("ลาก block วางข้าง/ล่าง · Shift+คลิกเลือกหลายชิ้น · Add to library", "ok");
    }
  }

  function prepareForPreview() {
    ensureElements();
    (stateRef.draft.layout.elements || []).forEach((el) => {
      if (el.type === "config-table") {
        const preset = resolveTablePreset(el);
        if (preset) {
          ensureWorkingPreset(el, preset);
          commitWorking(el, el.tablePreset);
        }
      }
      if (el.type === "header") {
        const preset = resolveHeaderPreset(el);
        if (preset) {
          ensureWorkingHeaderPreset(el, preset);
          commitHeaderWorking(el, el.headerPreset);
        }
      }
    });
    if (!stateRef.draft.manifest) stateRef.draft.manifest = {};
    stateRef.draft.manifest.layoutMode = "designer";
  }

  function isDesignerMode() {
    return isStudioCanvas();
  }

  global.TableDesigner = {
    isDesignerMode,
    isStudioCanvas,
    syncBodyClass,
    renderAll,
    onPackageOpened,
    prepareForPreview,
    ensureElements,
    promoteToDesignerIfNeeded,
    reflowElements,
    addConfigTable,
    addDataGrid,
    addHeader,
    openLibraryHeader,
    saveLibraryHeader,
    deleteLibraryHeader,
    openLibraryTable,
    saveLibraryTable,
    deleteLibraryTable,
    openLibraryFragment,
    saveLibraryFragment,
    deleteLibraryFragment,
    addBoxText,
    addNarrative,
    addPageOf,
    addFragmentPrompt,
    insertFragment,
    getCatalogSnapshot,
    getSelectedElement,
    getSelectedElementId: () => selectedElementId,
    getSelectedElementIds: () => getSelectedIdsInLayoutOrder(),
    addSelectionToLibrary,
    saveFragmentFromSelection,
    saveHeaderPresetFromElement,
    saveTablePresetFromElement,
    deleteElement,
    loadCatalogExtras,
  };

  global.TableDesigner.init = function (state, els, api, setStatus, schedulePreview) {
    stateRef = state;
    elsRef = els;
    apiRef = api;
    setStatusRef = setStatus;
    schedulePreviewRef = schedulePreview;

    if (global.DesignerCanvasTools) {
      canvasTools = global.DesignerCanvasTools.create({
        getHost: () => document.getElementById("designerCanvas"),
        isActive: () => isStudioCanvas() && isDesignerPackage(),
        getSnapshot: () => {
          ensureElements();
          return {
            page: stateRef.draft.layout.page,
            elements: stateRef.draft.layout.elements,
            selectedElementId,
            selectedElementIds: [...selectedElementIds],
          };
        },
        applySnapshot: (snap) => {
          ensureElements();
          // Fresh deep clone — never share refs with history stack strings' parse trees across edits.
          const page = snap.page ? JSON.parse(JSON.stringify(snap.page)) : stateRef.draft.layout.page;
          const elements = Array.isArray(snap.elements)
            ? JSON.parse(JSON.stringify(snap.elements))
            : stateRef.draft.layout.elements;
          stateRef.draft.layout.page = page;
          stateRef.draft.layout.elements = elements;
          if (Array.isArray(snap.selectedElementIds) && snap.selectedElementIds.length) {
            selectedElementIds = new Set(snap.selectedElementIds);
            selectedElementId = snap.selectedElementId && selectedElementIds.has(snap.selectedElementId)
              ? snap.selectedElementId
              : snap.selectedElementIds[0];
          } else {
            setSingleSelection(snap.selectedElementId || null);
          }
          lastFlow = null;
        },
        onViewChanged: () => renderAll(),
      });
      canvasTools.wire();
    }

    const addBtn = document.getElementById("btnAddConfigTable");
    if (addBtn) addBtn.addEventListener("click", () => addConfigTable());
    const addGridBtn = document.getElementById("btnAddDataGrid");
    if (addGridBtn) addGridBtn.addEventListener("click", () => addDataGrid());
    const addTblInner = document.getElementById("btnAddConfigTableInner");
    if (addTblInner) addTblInner.addEventListener("click", () => addConfigTable({ inner: true }));
    const hdrBtn = document.getElementById("btnAddHeader");
    if (hdrBtn) hdrBtn.addEventListener("click", () => addHeader());
    const boxBtn = document.getElementById("btnAddBoxText");
    if (boxBtn) boxBtn.addEventListener("click", () => addBoxText());
    const boxInner = document.getElementById("btnAddBoxTextInner");
    if (boxInner) boxInner.addEventListener("click", () => addBoxText({ inner: true }));
    const narrBtn = document.getElementById("btnAddNarrative");
    if (narrBtn) narrBtn.addEventListener("click", () => addNarrative());
    const pageOfBtn = document.getElementById("btnAddPageOf");
    if (pageOfBtn) pageOfBtn.addEventListener("click", () => addPageOf());
    const fragBtn = document.getElementById("btnAddFragment");
    if (fragBtn) fragBtn.addEventListener("click", () => addFragmentPrompt());
    const addLibBtn = document.getElementById("btnAddToLibrary");
    if (addLibBtn) {
      addLibBtn.addEventListener("click", () => {
        addSelectionToLibrary().catch((err) => {
          if (setStatusRef) setStatusRef(err.message || String(err), "err");
          else alert(err.message || err);
        });
      });
    }
  };
})(window);
