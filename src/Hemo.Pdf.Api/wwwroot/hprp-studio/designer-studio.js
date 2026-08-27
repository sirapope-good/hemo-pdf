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
  const MIN_COL_WEIGHT = 0.25;

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
  let adapterSchema = null;
  let sampleData = null;
  let dragState = null;
  let suppressClick = false;

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
    const spacing = Number(page.spacingMm != null ? page.spacingMm : 2);
    const scale = DISPLAY_W / pageW;
    return { page, pageW, pageH, margins, contentW, contentH, spacing, scale, landscape };
  }

  /**
   * Pack elements into content box without overlap.
   * place: "beside" → same row to the right of previous; else stack below.
   */
  function reflowElements() {
    ensureElements();
    const { contentW, spacing } = pageMetrics();
    const els = stateRef.draft.layout.elements;
    let cursorY = 0;
    let rowStart = 0;
    let i = 0;
    while (i < els.length) {
      const row = [els[i]];
      let j = i + 1;
      while (j < els.length && String(els[j].place || "below").toLowerCase() === "beside") {
        row.push(els[j]);
        j++;
      }

      const gapTotal = spacing * Math.max(0, row.length - 1);
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

      let maxH = 0;
      let x = 0;
      row.forEach((e) => {
        if (!e.manualWidth) e.box.wMm = Math.max(MIN_BLOCK_W, autoW);
        e.box.hMm = Math.max(MIN_BLOCK_H, Number(e.box.hMm) || MIN_BLOCK_H);
        e.box.xMm = x;
        e.box.yMm = cursorY;
        x += e.box.wMm + spacing;
        maxH = Math.max(maxH, e.box.hMm);
      });
      // Equalize row height for beside siblings
      row.forEach((e) => { e.box.hMm = maxH; });

      cursorY += maxH + spacing;
      i = j;
      rowStart = i;
    }
  }

  function promoteToDesignerIfNeeded() {
    ensureElements();
    const manifest = stateRef.draft.manifest || (stateRef.draft.manifest = {});
    const layout = stateRef.draft.layout;

    if (isDesignerPackage() && layout.elements.length > 0) {
      reflowElements();
      return false;
    }

    const body = layout.body || [];
    const hasAnnual = body.some((n) => n && n.widget === "clinical.hct-epo-annual-table");
    const hasCopay = body.some((n) => n && n.widget === "clinical.hct-epo-copay");
    const isClinical01 =
      String(manifest.id || "").indexOf("clinical-01-hct-epo") === 0
      || String(manifest.dataAdapter || "") === "clinical-01-hct-epo"
      || hasAnnual;

    if (isClinical01 && layout.elements.length === 0) {
      layout.elements = [
        {
          id: "hdr",
          type: "header",
          preset: "thaiur-header-v1",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 27 },
        },
        {
          id: "annual",
          type: "config-table",
          presetId: "hct-epo-annual-v1",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 228 },
          bindings: CLINICAL01_ANNUAL_BINDINGS.slice(),
          chrome: { border: "thin", headerFill: "$branding.sectionHeaderBackground" },
        },
      ];
      if (hasCopay || isClinical01) {
        layout.elements.push({
          id: "copay",
          type: "dense",
          widget: "clinical.hct-epo-copay",
          place: "below",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 34 },
          chrome: { headerFill: "$branding.sectionHeaderBackground", border: "thin" },
        });
      }
    }

    if (layout.elements.length === 0) {
      layout.elements.push({
        id: "tbl_main",
        type: "config-table",
        presetId: "hct-epo-annual-v1",
        place: "below",
        box: { xMm: 0, yMm: 0, wMm: 206, hMm: 200 },
        bindings: [],
        chrome: { border: "thin" },
      });
    }

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
    try {
      const presets = await apiRef("/api/hprp/presets/tables");
      tablePresets = {};
      (presets || []).forEach((p) => { tablePresets[p.id] = p; });
    } catch (_) { /* optional */ }

    const adapter = stateRef.draft.manifest && stateRef.draft.manifest.dataAdapter;
    if (adapter) {
      try {
        adapterSchema = await apiRef(`/api/hprp/adapters/${encodeURIComponent(adapter)}/schema`);
      } catch (_) {
        adapterSchema = null;
      }
    }
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
    try {
      sampleData = await apiRef(`/api/hprp/packages/${encodeURIComponent(item.id)}/sample-data${q}`);
    } catch (_) {
      if (String(item.id).indexOf("clinical-01") === 0 && item.id !== "clinical-01-hct-epo") {
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
    const { page, pageW, pageH, margins, contentW, contentH, scale, landscape } = m;

    const sheet = document.createElement("div");
    sheet.className = "designer-sheet" + (landscape ? " landscape" : "");
    sheet.style.width = DISPLAY_W + "px";
    sheet.style.height = pageH * scale + "px";
    if (String(page.border || "none").toLowerCase() === "thin")
      sheet.classList.add("has-page-border");

    // Margin guides
    const guide = document.createElement("div");
    guide.className = "designer-margin-guide";
    guide.style.left = margins.left * scale + "px";
    guide.style.top = margins.top * scale + "px";
    guide.style.width = contentW * scale + "px";
    guide.style.height = contentH * scale + "px";
    sheet.appendChild(guide);

    const dropHint = document.createElement("div");
    dropHint.className = "designer-drop-hint hidden";
    dropHint.id = "designerDropHint";
    sheet.appendChild(dropHint);

    sheet.addEventListener("click", () => {
      if (suppressClick) return;
      selectedElementId = null;
      stateRef.selectedKey = null;
      renderInspector();
      document.querySelectorAll(".designer-element.selected").forEach((n) => n.classList.remove("selected"));
    });

    const lang = Object.keys(stateRef.draft.labels || {})[0] || "th";
    const labels = (stateRef.draft.labels && stateRef.draft.labels[lang]) || {};

    stateRef.draft.layout.elements.forEach((el, index) => {
      const box = el.box;
      const wrap = document.createElement("div");
      wrap.className = "designer-element" + (el.id === selectedElementId ? " selected" : "");
      if (!borderOn(el.chrome)) wrap.classList.add("no-border");
      wrap.style.left = (margins.left + box.xMm) * scale + "px";
      wrap.style.top = (margins.top + box.yMm) * scale + "px";
      wrap.style.width = box.wMm * scale + "px";
      wrap.style.height = box.hMm * scale + "px";
      wrap.dataset.elementId = el.id;
      wrap.dataset.index = String(index);

      wrap.addEventListener("click", (e) => {
        e.stopPropagation();
        if (suppressClick) return;
        selectedElementId = el.id;
        stateRef.selectedKey = null;
        renderAll();
      });

      // Drag move (not from handles)
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
      toolbar.innerHTML = `<span class="el-tag">${escapeHtml(el.type)}</span>`;
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
      if (el.type === "config-table") {
        const catalogOrInline = resolveTablePreset(el);
        if (catalogOrInline && global.TableLayoutEngine) {
          // Always detach inline so Download PDF uses the same weights as canvas.
          const preset = ensureWorkingPreset(el, catalogOrInline);
          const model = global.TableLayoutEngine.buildLayout(preset, el, labels, sampleData, box.hMm);
          body.appendChild(renderTableHtml(model, el, scale));
        } else {
          body.innerHTML = `<div class="ph-dense">config-table</div>`;
        }
      } else if (el.type === "header") {
        const patient = sampleData && sampleData.header && sampleData.header.patient;
        const title = (sampleData && sampleData.title) || "Header";
        body.classList.add("designer-header-placeholder");
        body.innerHTML =
          `<div class="ph-title">${escapeHtml(title)}</div>` +
          `<div class="ph-meta">${escapeHtml((patient && patient.name) || "Patient")} · HN ${escapeHtml((patient && patient.hn) || "—")}</div>`;
      } else {
        body.innerHTML = `<div class="ph-dense">${escapeHtml(el.type)}: ${escapeHtml(el.widget || el.id)}</div>`;
      }
      wrap.appendChild(body);

      // Resize handles
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

    host.appendChild(sheet);
  }

  /**
   * HTML table that mirrors ConfigurableTableComposer (QuestPDF):
   * - Header: DATE (month+day width) | data columns
   * - Body: month rowspan | day | data columns
   * - Row heights locked to layout engine mm (parity with Download PDF)
   */
  function renderTableHtml(model, el, scale) {
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

    // Exact mm→px so canvas height equals PDF box (no leftover white gap).
    const headerPx = Math.max(4, model.headerHeightMm * scale);
    const slotPx = Math.max(3, model.slotHeightMm * scale);
    const bodyRowCount = Math.max(1, (model.rows && model.rows.length) || 1);
    const tablePx = headerPx + slotPx * bodyRowCount;
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
    const startX = e.clientX;
    const startY = e.clientY;
    const startW = el.box.wMm;
    const startH = el.box.hMm;
    const { contentW } = metrics;

    function onMove(ev) {
      const dx = (ev.clientX - startX) / metrics.scale;
      const dy = (ev.clientY - startY) / metrics.scale;
      if (dir === "e" || dir === "se") {
        el.box.wMm = Math.max(MIN_BLOCK_W, Math.min(contentW - el.box.xMm, startW + dx));
        el.manualWidth = true;
      }
      if (dir === "s" || dir === "se") {
        el.box.hMm = Math.max(MIN_BLOCK_H, startH + dy);
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
      el.place = placeSel.value;
      reflowElements();
      renderAll();
    });
    placeLab.appendChild(placeSel);
    insp.appendChild(placeLab);

    const borderLab = document.createElement("label");
    borderLab.className = "check-lab";
    const borderCb = document.createElement("input");
    borderCb.type = "checkbox";
    borderCb.checked = borderOn(el.chrome);
    borderCb.addEventListener("change", () => {
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
      el.manualWidth = false;
      reflowElements();
      renderAll();
    });
    insp.appendChild(fitBtn);

    if (el.type === "config-table") {
      renderTableInspector(insp, el);
    }

    renderBoxFields(insp, el);
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

    const sp = document.createElement("label");
    sp.textContent = "spacingMm (ระหว่าง block)";
    const spi = document.createElement("input");
    spi.type = "number";
    spi.min = "0";
    spi.step = "0.5";
    spi.value = String(page.spacingMm != null ? page.spacingMm : 2);
    spi.addEventListener("change", () => {
      page.spacingMm = Number(spi.value);
      reflowElements();
      renderAll();
    });
    sp.appendChild(spi);
    insp.appendChild(sp);

    const borderLab = document.createElement("label");
    borderLab.className = "check-lab";
    const borderCb = document.createElement("input");
    borderCb.type = "checkbox";
    borderCb.checked = String(page.border || "none").toLowerCase() === "thin";
    borderCb.addEventListener("change", () => {
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
      input.value = String((el.box && el.box[key]) || 0);
      input.addEventListener("change", () => {
        el.box = el.box || {};
        el.box[key] = Number(input.value);
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
    const id = "tbl_" + Math.random().toString(36).slice(2, 7);
    stateRef.draft.layout.elements.push({
      id,
      type: "config-table",
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
    if (setStatusRef) {
      setStatusRef("ลาก block วางข้าง/ล่าง · ลากขอบย่อขยาย · Page สำหรับ margin/ขอบ", "ok");
    }
  }

  function prepareForPreview() {
    ensureElements();
    (stateRef.draft.layout.elements || []).forEach((el) => {
      if (el.type !== "config-table") return;
      const preset = resolveTablePreset(el);
      if (preset) {
        ensureWorkingPreset(el, preset);
        commitWorking(el, el.tablePreset);
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
    deleteElement,
    getSelectedElementId: () => selectedElementId,
  };

  global.TableDesigner.init = function (state, els, api, setStatus, schedulePreview) {
    stateRef = state;
    elsRef = els;
    apiRef = api;
    setStatusRef = setStatus;
    schedulePreviewRef = schedulePreview;

    const addBtn = document.getElementById("btnAddConfigTable");
    if (addBtn) addBtn.addEventListener("click", () => addConfigTable());
  };
})(window);
