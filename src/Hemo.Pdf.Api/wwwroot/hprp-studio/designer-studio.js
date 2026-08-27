/**
 * Designer layoutMode: WYSIWYG HTML canvas (no separate preview pane).
 */
(function (global) {
  const DISPLAY_W = 420;
  const A4_W = 210;
  const A4_H = 297;

  let stateRef;
  let elsRef;
  let apiRef;
  let setStatusRef;
  let schedulePreviewRef;
  let selectedElementId = null;
  let tablePresets = {};
  let adapterSchema = null;
  let sampleData = null;

  function isDesignerMode() {
    return String((stateRef.draft.manifest && stateRef.draft.manifest.layoutMode) || "").toLowerCase() === "designer";
  }

  function ensureElements() {
    if (!stateRef.draft.layout.elements) stateRef.draft.layout.elements = [];
    if (!stateRef.draft.layout.page) stateRef.draft.layout.page = { size: "A4", marginMm: 2 };
  }

  function mmScale(page) {
    const landscape = String(page.orientation || "").toLowerCase() === "landscape";
    const w = landscape ? A4_H : A4_W;
    const h = landscape ? A4_W : A4_H;
    return { w, h, scale: DISPLAY_W / w, landscape };
  }

  function selectedElement() {
    ensureElements();
    return stateRef.draft.layout.elements.find((e) => e.id === selectedElementId) || null;
  }

  function resolveTablePreset(el) {
    if (el.tablePreset && el.tablePreset.id) return el.tablePreset;
    if (el.presetId && tablePresets[el.presetId]) return tablePresets[el.presetId];
    return null;
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
      sampleData = await apiRef(
        `/api/hprp/packages/${encodeURIComponent(item.id)}/sample-data${q}`);
    } catch (_) {
      sampleData = null;
    }
  }

  function renderCanvas() {
    const host = document.getElementById("designerCanvas");
    if (!host || !isDesignerMode()) return;
    ensureElements();
    host.innerHTML = "";
    const page = stateRef.draft.layout.page;
    const { w, h, scale, landscape } = mmScale(page);
    const sheet = document.createElement("div");
    sheet.className = "designer-sheet" + (landscape ? " landscape" : "");
    sheet.style.width = DISPLAY_W + "px";
    sheet.style.height = h * scale + "px";

    const toolbar = document.createElement("div");
    toolbar.className = "designer-canvas-toolbar";
    toolbar.innerHTML = `<span class="muted">A4 ${landscape ? "landscape" : "portrait"} · HTML WYSIWYG</span>`;
    const btnPdf = document.createElement("button");
    btnPdf.type = "button";
    btnPdf.textContent = "Download PDF";
    btnPdf.addEventListener("click", () => schedulePreviewRef && schedulePreviewRef(true));
    toolbar.appendChild(btnPdf);
    sheet.appendChild(toolbar);

    const lang = Object.keys(stateRef.draft.labels || {})[0] || "th";
    const labels = (stateRef.draft.labels && stateRef.draft.labels[lang]) || {};

    stateRef.draft.layout.elements.forEach((el) => {
      const box = el.box || { xMm: 0, yMm: 0, wMm: 100, hMm: 40 };
      const wrap = document.createElement("div");
      wrap.className = "designer-element" + (el.id === selectedElementId ? " selected" : "");
      wrap.style.left = box.xMm * scale + "px";
      wrap.style.top = box.yMm * scale + "px";
      wrap.style.width = box.wMm * scale + "px";
      wrap.style.height = box.hMm * scale + "px";
      wrap.dataset.elementId = el.id;
      wrap.addEventListener("click", (e) => {
        e.stopPropagation();
        selectedElementId = el.id;
        renderAll();
      });

      if (el.type === "config-table") {
        const preset = resolveTablePreset(el);
        if (preset && global.TableLayoutEngine) {
          const model = global.TableLayoutEngine.buildLayout(preset, el, labels, sampleData, box.hMm);
          wrap.appendChild(renderTableHtml(model, scale));
        } else {
          wrap.textContent = "config-table (missing preset)";
        }
      } else if (el.type === "header") {
        wrap.classList.add("designer-header-placeholder");
        wrap.innerHTML = `<div class="ph-title">${(sampleData && sampleData.title) || "Header"}</div><div class="ph-meta">ThaiUR header · ${el.preset || "default"}</div>`;
      } else {
        wrap.innerHTML = `<div class="ph-dense">${el.type}: ${el.widget || el.id}</div>`;
      }

      sheet.appendChild(wrap);
    });

    host.appendChild(sheet);
  }

  function renderTableHtml(model, scale) {
    const root = document.createElement("div");
    root.className = "cfg-table";
    const p = model.preset;
    const dateW = (p.dateColumns.monthWeight + p.dateColumns.dayWeight) / (p.dateColumns.monthWeight + p.dateColumns.dayWeight + p.columns.reduce((s, c) => s + c.weight, 0));

    const table = document.createElement("table");
    table.cellSpacing = 0;
    table.cellPadding = 0;

    const thead = document.createElement("thead");
    const hr = document.createElement("tr");
    model.headerLabels.forEach((text, i) => {
      const th = document.createElement("th");
      th.textContent = text;
      if (i === 0) th.colSpan = 1;
      hr.appendChild(th);
    });
    thead.appendChild(hr);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");
    if (p.rowMode === "freedom") {
      model.rows.forEach((row) => {
        const tr = document.createElement("tr");
        row.cells.forEach((cell) => {
          const td = document.createElement("td");
          td.textContent = cell.text;
          if (cell.historical) td.className = "historical";
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
    } else {
      let g = -1;
      model.rows.forEach((row) => {
        const tr = document.createElement("tr");
        if (row.groupIndex !== g) {
          g = row.groupIndex;
          if (row.slotIndex === 0 && row.groupLabel) {
            const tdMonth = document.createElement("td");
            tdMonth.rowSpan = p.slotsPerGroup;
            tdMonth.className = "month-cell";
            tdMonth.textContent = row.groupLabel;
            tr.appendChild(tdMonth);
          }
        }
        row.cells.forEach((cell, ci) => {
          const td = document.createElement("td");
          td.textContent = cell.text;
          if (cell.historical) td.className = "historical";
          if (ci > 0 && cell.center) td.className = (td.className + " center").trim();
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
    }

    table.appendChild(tbody);
    root.appendChild(table);
    return root;
  }

  function renderInspector() {
    const insp = elsRef.inspector;
    if (!insp || !isDesignerMode()) return;
    insp.innerHTML = "";
    const el = selectedElement();
    if (!el) {
      insp.innerHTML = "<p class=\"muted\">คลิก element บน canvas หรือเพิ่ม config-table จาก palette</p>";
      renderPaletteButtons(insp);
      return;
    }

    insp.appendChild(Object.assign(document.createElement("p"), {
      innerHTML: `<strong>${el.type}</strong> <span class="muted">${el.id}</span>`,
    }));

    if (el.type !== "config-table") {
      renderBoxFields(insp, el);
      return;
    }

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

    const working = el.tablePreset || preset;

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
      if (el.tablePreset) el.tablePreset = working;
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
      if (el.tablePreset) el.tablePreset = working;
      renderAll();
    });
    slotsLabel.appendChild(slotsInput);
    insp.appendChild(slotsLabel);

    const colHead = document.createElement("p");
    colHead.innerHTML = "<strong>Columns</strong>";
    insp.appendChild(colHead);

    (working.columns || []).forEach((col, idx) => {
      const row = document.createElement("div");
      row.className = "col-row";
      row.innerHTML = `<span>${col.id}</span>`;
      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "−";
      del.addEventListener("click", () => {
        working.columns.splice(idx, 1);
        if (el.tablePreset) el.tablePreset = working;
        renderAll();
      });
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
      if (el.tablePreset) {
        el.tablePreset = working;
        el.presetId = undefined;
      }
      renderAll();
    });
    insp.appendChild(addCol);

    renderBindings(insp, el);
    renderPresetActions(insp, el, working);
    renderBoxFields(insp, el);
  }

  function renderBindings(insp, el) {
    const head = document.createElement("p");
    head.innerHTML = "<strong>Bindings</strong>";
    insp.appendChild(head);
    (el.bindings || []).forEach((b, i) => {
      const row = document.createElement("div");
      row.className = "bind-row muted";
      row.textContent = `${b.column} ← ${b.path} (${b.context})`;
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
    const leaf = path.split(".").pop().replace("[]", "");
    return leaf;
  }

  function renderPresetActions(insp, el, working) {
    const save = document.createElement("button");
    save.type = "button";
    save.textContent = "Save as preset";
    save.addEventListener("click", async () => {
      const id = prompt("Preset id", working.id || "my-table-preset");
      if (!id) return;
      const body = { ...working, id, displayName: working.displayName || id };
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
      const id = prompt("Preset id", el.presetId || "");
      if (!id || !tablePresets[id]) return;
      el.presetId = id;
      el.tablePreset = undefined;
      renderAll();
    });
    insp.appendChild(load);
  }

  function renderBoxFields(insp, el) {
    ["xMm", "yMm", "wMm", "hMm"].forEach((key) => {
      const lab = document.createElement("label");
      lab.textContent = "box." + key;
      const input = document.createElement("input");
      input.type = "number";
      input.step = "1";
      input.value = String((el.box && el.box[key]) || 0);
      input.addEventListener("change", () => {
        el.box = el.box || {};
        el.box[key] = Number(input.value);
        renderAll();
      });
      lab.appendChild(input);
      insp.appendChild(lab);
    });
  }

  function renderPaletteButtons(host) {
    const add = document.createElement("button");
    add.type = "button";
    add.textContent = "+ Config table (annual preset)";
    add.addEventListener("click", () => {
      ensureElements();
      const id = "tbl_" + Math.random().toString(36).slice(2, 7);
      stateRef.draft.layout.elements.push({
        id,
        type: "config-table",
        presetId: "hct-epo-annual-v1",
        box: { xMm: 0, yMm: 30, wMm: 206, hMm: 200 },
        bindings: [],
      });
      selectedElementId = id;
      renderAll();
    });
    host.appendChild(add);
  }

  function renderPalette() {
    const host = document.getElementById("designerPalette");
    if (!host) return;
    host.innerHTML = "";
    renderPaletteButtons(host);
  }

  function syncBodyClass() {
    document.body.classList.toggle("mode-designer-layout", isDesignerMode());
    document.body.classList.toggle("mode-composition", !isDesignerMode() && stateRef.mode === "designer");
    const pane = document.getElementById("designerStudioPane");
    if (pane) pane.classList.toggle("hidden", !isDesignerMode());
  }

  function renderAll() {
    if (!isDesignerMode()) return;
    syncBodyClass();
    renderPalette();
    renderCanvas();
    renderInspector();
  }

  async function onPackageOpened() {
    if (!isDesignerMode()) return;
    selectedElementId = null;
    await loadCatalogExtras();
    await loadSampleData();
    renderAll();
  }

  global.TableDesigner = {
    isDesignerMode,
    syncBodyClass,
    renderAll,
    onPackageOpened,
    ensureElements,
    getSelectedElementId: () => selectedElementId,
  };

  global.TableDesigner.init = function (state, els, api, setStatus, schedulePreview) {
    stateRef = state;
    elsRef = els;
    apiRef = api;
    setStatusRef = setStatus;
    schedulePreviewRef = schedulePreview;
  };
})(window);
