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

  function resolveBand(el) {
    const b = String(el.band || "").toLowerCase().trim();
    if (BANDS.indexOf(b) >= 0) return b;
    const t = String(el.type || "").toLowerCase();
    if (t === "header") return "header";
    if (t === "page-of") return "super-footer";
    return "content";
  }

  function minHeightForElement(el) {
    const t = String(el && el.type || "").toLowerCase();
    return t === "box-text" || t === "page-of" ? MIN_BOX_TEXT_H : MIN_BLOCK_H;
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

  /** Max width for resize: row position + trailing siblings must fit in contentW. */
  function maxWidthInRow(el, contentW, margins, gaps, scale) {
    const els = stateRef.draft.layout.elements;
    const idx = els.indexOf(el);
    if (idx < 0) return contentW;
    let start = idx;
    while (start > 0 && String(els[start].place || "below").toLowerCase() === "beside") start--;
    let end = idx;
    while (end + 1 < els.length && String(els[end + 1].place || "below").toLowerCase() === "beside") end++;
    let relX = 0;
    for (let i = start; i < idx; i++)
      relX += gapStep(Math.max(MIN_BLOCK_W, Number(els[i].box.wMm) || MIN_BLOCK_W), gaps.beside, scale);
    let tailW = 0;
    for (let i = idx + 1; i <= end; i++)
      tailW += gapStep(Math.max(MIN_BLOCK_W, Number(els[i].box.wMm) || MIN_BLOCK_W), gaps.beside, scale);
    return Math.max(MIN_BLOCK_W, contentW - relX - tailW);
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
   */
  function reflowElements() {
    ensureElements();
    const { contentW, contentH, gaps, pageH, margins, scale } = pageMetrics();
    const els = stateRef.draft.layout.elements;
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
        let maxRowH = 0;
        row.forEach((e) => {
          const minH = minHeightForElement(e);
          e.box.hMm = Math.max(minH, Number(e.box.hMm) || minH);
          maxRowH = Math.max(maxRowH, e.box.hMm);
        });
        if (cursorY + maxRowH > maxH + 0.01 && result.length > 0) break;

        const remain = Math.max(MIN_BLOCK_W * autoCount, contentW - fixedW - gapTotal);
        const autoW = autoCount > 0 ? remain / autoCount : 0;
        let x = 0;
        row.forEach((e) => {
          if (!e.manualWidth) e.box.wMm = Math.max(MIN_BLOCK_W, autoW);
          // Keep each block's own height — only maxRowH drives vertical row advance.
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

    const pageCount = Math.max(1, contentPages.length);
    const pages = [];
    for (let p = 0; p < pageCount; p++) {
      const pageEls = [];
      function placeBandAbs(band, originX, originY) {
        band.forEach((e) => {
          pageEls.push({
            el: e,
            xMm: originX + e.box.xMm,
            yMm: originY + e.box.yMm,
            wMm: e.box.wMm,
            hMm: e.box.hMm,
            outsideMargin: originY < guideTop - 0.01 || originY >= guideTop + guideHeight - 0.01,
          });
        });
      }

      placeBandAbs(superHeader, margins.left, guideTop - sh);
      let innerY = guideTop;
      placeBandAbs(header, margins.left, innerY);
      innerY += headerH;
      (contentPages[p] || []).forEach((e) => {
        pageEls.push({
          el: e,
          xMm: margins.left + e.box.xMm,
          yMm: innerY + e.box.yMm,
          wMm: e.box.wMm,
          hMm: e.box.hMm,
          outsideMargin: false,
        });
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

  function promoteToDesignerIfNeeded() {
    ensureElements();
    const manifest = stateRef.draft.manifest || (stateRef.draft.manifest = {});
    const layout = stateRef.draft.layout;

    if (isDesignerPackage() && layout.elements.length > 0) {
      migrateLegacyDenseCopay();
      reflowElements();
      return false;
    }

    const body = layout.body || [];
    const hasAnnual = body.some((n) => n && n.widget === "clinical.hct-epo-annual-table");
    const isClinical01 =
      String(manifest.id || "").indexOf("clinical-01-hct-epo") === 0
      || String(manifest.dataAdapter || "") === "clinical-01-hct-epo"
      || hasAnnual;

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
    return stateRef.draft.layout.elements.find((e) => e.id === selectedElementId) || null;
  }

  function resolveTablePreset(el) {
    if (el.tablePreset && (el.tablePreset.id || (el.tablePreset.columns && el.tablePreset.columns.length)))
      return el.tablePreset;
    if (el.presetId && tablePresets[el.presetId]) return tablePresets[el.presetId];
    return el.tablePreset || null;
  }

  async function loadCatalogExtras() {
    if (!apiRef) return;
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

    if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
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
      (stateRef.libraryEdit && stateRef.libraryEdit.kind === "headers")
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
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
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
      addBandGuide("content", flow.contentFlowH || 0, "band-content");
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
        selectedElementId = null;
        stateRef.selectedKey = null;
        renderInspector();
        document.querySelectorAll(".designer-element.selected").forEach((n) => n.classList.remove("selected"));
      });

      (flow.pages[p] || []).forEach((item, index) => {
        const el = item.el;
        const wrap = document.createElement("div");
        wrap.className = "designer-element" + (el.id === selectedElementId ? " selected" : "");
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

        wrap.addEventListener("click", (e) => {
          e.stopPropagation();
          if (suppressClick) return;
          selectedElementId = el.id;
          stateRef.selectedKey = null;
          renderAll();
        });

        wrap.addEventListener("pointerdown", (e) => {
          if (e.target.closest(".resize-handle") || e.target.closest(".col-resize") || e.target.closest(".el-toolbar"))
            return;
          if (e.button !== 0) return;
          e.preventDefault();
          e.stopPropagation();
          selectedElementId = el.id;
          startMoveDrag(e, el, wrap, sheet, m);
        });

        const toolbar = document.createElement("div");
        toolbar.className = "el-toolbar";
        toolbar.innerHTML =
          `<span class="el-tag">${escapeHtml(el.type)} · ${escapeHtml(resolveBand(el))}</span>`;
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
          if (catalogOrInline && global.TableLayoutEngine) {
            const preset = ensureWorkingPreset(el, catalogOrInline);
            const model = global.TableLayoutEngine.buildLayout(preset, el, labels, sampleData, item.hMm);
            body.appendChild(renderTableHtml(model, el, scale, item.hMm));
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
            const model = global.HeaderLayoutEngine.buildLayout(preset, sampleData, titleFallback);
            body.appendChild(renderHeaderHtml(model, el, scale));
          } else {
            const patient = sampleData && sampleData.header && sampleData.header.patient;
            const title = (sampleData && sampleData.title) || "Header";
            body.classList.add("designer-header-placeholder");
            body.innerHTML =
              `<div class="ph-title">${escapeHtml(title)}</div>` +
              `<div class="ph-meta">${escapeHtml((patient && patient.name) || "Patient")} · HN ${escapeHtml((patient && patient.hn) || "—")}</div>`;
          }
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
      model.rows.forEach((row) => {
        const tr = document.createElement("tr");
        tr.style.height = slotPx.toFixed(2) + "px";
        row.cells.forEach((cell) => {
          const td = document.createElement("td");
          td.textContent = cellText(cell.text, nbsp);
          if (cell.historical) td.className = "historical";
          if (cell.center) td.classList.add("center");
          tr.appendChild(td);
        });
        for (let i = row.cells.length; i < cols.length; i++) {
          const td = document.createElement("td");
          td.textContent = nbsp;
          tr.appendChild(td);
        }
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
    return root;
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

  function renderHeaderHtml(model, el, scale) {
    const root = document.createElement("div");
    root.className = "cfg-header" + (borderOn(el.chrome) || borderOn(model.preset.chrome) ? "" : " cfg-no-border");
    const preset = model.preset;
    const cols = preset.columns || [];
    const wMm = Math.max(1, el.box.wMm);
    const fracs = global.HeaderLayoutEngine.bandFractions(cols, wMm);
    const titlePx = Math.max(8, model.titleRowHeightMm * scale);
    const bottomPx = Math.max(6, model.bottomRowHeightMm * scale);
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

    const bottom = document.createElement("div");
    bottom.className = "cfg-header-bottom";
    bottom.style.height = bottomPx.toFixed(2) + "px";
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
    root.appendChild(bottom);

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
    const els = stateRef.draft.layout.elements;
    const from = els.indexOf(el);
    if (from < 0) return;
    els.splice(from, 1);

    if (!targetNode || mode === "end") {
      el.place = "below";
      els.push(el);
      reflowElements();
      return;
    }

    const targetId = targetNode.dataset.elementId;
    let to = els.findIndex((e) => e.id === targetId);
    if (to < 0) {
      els.push(el);
      reflowElements();
      return;
    }

    if (mode === "beside") {
      el.place = "beside";
      els.splice(to + 1, 0, el);
    } else {
      el.place = "below";
      // clear beside on element that was after target if we insert between rows
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

    function onMove(ev) {
      const dx = (ev.clientX - startX) / metrics.scale;
      const dy = (ev.clientY - startY) / metrics.scale;
      if (dir === "e" || dir === "se") {
        const maxW = maxWidthInRow(el, contentW, margins, gaps, metrics.scale);
        el.box.wMm = Math.max(MIN_BLOCK_W, Math.min(maxW, startW + dx));
        el.manualWidth = true;
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
    const els = stateRef.draft.layout.elements;
    const idx = els.findIndex((e) => e.id === id);
    if (idx < 0) return;
    els.splice(idx, 1);
    if (els[idx] && String(els[idx].place).toLowerCase() === "beside") {
      // first of a row after delete should be below
      els[idx].place = "below";
    }
    if (selectedElementId === id) selectedElementId = null;
    reflowElements();
    renderAll();
    if (setStatusRef) setStatusRef("ลบ widget แล้ว", "ok");
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
      insp.innerHTML = "<p class=\"muted\">คลิก block บน canvas · ลากวางข้าง/ล่าง · ลากขอบขวา/ล่างเพื่อย่อขยาย</p>";
      return;
    }

    const head = document.createElement("div");
    head.className = "insp-head";
    head.innerHTML = `<strong>${escapeHtml(el.type)}</strong> <span class="muted">${escapeHtml(el.id)}</span>`;
    insp.appendChild(head);

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
    if (el.type === "header") {
      renderHeaderInspector(insp, el);
    }
    if (el.type === "box-text") {
      renderBoxTextInspector(insp, el);
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
    ["annual", "monthly", "freedom"].forEach((m) => {
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
      name.value = col.id;
      name.addEventListener("change", () => {
        col.id = name.value.trim() || col.id;
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
      row.appendChild(del);
      insp.appendChild(row);
    });

    const addCol = document.createElement("button");
    addCol.type = "button";
    addCol.textContent = "+ Column";
    addCol.addEventListener("click", () => {
      const id = "col_" + Math.random().toString(36).slice(2, 6);
      working.columns = working.columns || [];
      working.columns.push({ id, labelKey: id, title: id, weight: 1, center: false, isLab: false });
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

  function addConfigTable() {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const id = "tbl_" + Math.random().toString(36).slice(2, 7);
    stateRef.draft.layout.elements.push({
      id,
      type: "config-table",
      band: "content",
      presetId: "hct-epo-annual-v1",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 100, hMm: 80 },
      bindings: [],
      chrome: { border: "thin" },
    });
    stateRef.draft.manifest.layoutMode = "designer";
    selectedElementId = id;
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
    selectedElementId = id;
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
    await loadCatalogExtras();
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
    selectedElementId = clones[0].id;
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
    ensureElements();
    return (stateRef.draft.layout.elements || []).find((e) => e.id === selectedElementId) || null;
  }

  function saveFragmentFromSelection(ids) {
    return (async () => {
      ensureElements();
      const want = Array.isArray(ids) && ids.length
        ? ids
        : selectedElementId
          ? [selectedElementId]
          : [];
      if (!want.length) {
        setStatusRef("Select element(s) first", "err");
        return;
      }
      const els = stateRef.draft.layout.elements.filter((e) => want.indexOf(e.id) >= 0);
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
      await apiRef(`/api/hprp/presets/fragments/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
      fragmentPresets[id] = body;
      setStatusRef("Saved fragment " + id, "ok");
      if (global.LibraryStudio && typeof global.LibraryStudio.refresh === "function") {
        global.LibraryStudio.refresh();
      }
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

  function addBoxText() {
    ensureElements();
    promoteToDesignerIfNeeded();
    if (canvasTools) canvasTools.pushHistory();
    const id = "box_" + Math.random().toString(36).slice(2, 7);
    stateRef.draft.layout.elements.push({
      id,
      type: "box-text",
      band: "content",
      place: "below",
      box: { xMm: 0, yMm: 0, wMm: 206, hMm: 5 },
      text: "หัวข้อ",
      align: "center",
      chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground", fontSize: 7.5 },
    });
    stateRef.draft.manifest.layoutMode = "designer";
    selectedElementId = id;
    stateRef.selectedKey = null;
    reflowElements();
    renderAll();
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
    selectedElementId = id;
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
    selectedElementId = null;
    stateRef.selectedKey = null;
    promoteToDesignerIfNeeded();
    await loadCatalogExtras();
    await loadSampleData();
    renderAll();
    if (canvasTools) canvasTools.resetHistory();
    if (setStatusRef) {
      setStatusRef("ลาก block วางข้าง/ล่าง · ลากขอบย่อขยาย · Page สำหรับ margin/ขอบ", "ok");
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
    addHeader,
    openLibraryHeader,
    saveLibraryHeader,
    addBoxText,
    addPageOf,
    addFragmentPrompt,
    insertFragment,
    getCatalogSnapshot,
    getSelectedElement,
    getSelectedElementId: () => selectedElementId,
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
          selectedElementId = snap.selectedElementId || null;
          lastFlow = null;
        },
        onViewChanged: () => renderAll(),
      });
      canvasTools.wire();
    }

    const addBtn = document.getElementById("btnAddConfigTable");
    if (addBtn) addBtn.addEventListener("click", () => addConfigTable());
    const hdrBtn = document.getElementById("btnAddHeader");
    if (hdrBtn) hdrBtn.addEventListener("click", () => addHeader());
    const boxBtn = document.getElementById("btnAddBoxText");
    if (boxBtn) boxBtn.addEventListener("click", () => addBoxText());
    const pageOfBtn = document.getElementById("btnAddPageOf");
    if (pageOfBtn) pageOfBtn.addEventListener("click", () => addPageOf());
    const fragBtn = document.getElementById("btnAddFragment");
    if (fragBtn) fragBtn.addEventListener("click", () => addFragmentPrompt());
  };
})(window);
