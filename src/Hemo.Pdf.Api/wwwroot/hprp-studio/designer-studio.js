/**
 * HPRP Studio — WYSIWYG page canvas (no tree, no PDF preview pane).
 * Canvas HTML is the primary editor; Download PDF verifies QuestPDF only.
 */
(function (global) {
  const DISPLAY_W = 520;
  const A4_W = 210;
  const A4_H = 297;

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

  /** Studio Canvas mode (not JSON) — always WYSIWYG on this branch. */
  function isStudioCanvas() {
    return !stateRef || stateRef.mode !== "json";
  }

  /** Package uses designer layout elements (or was auto-promoted). */
  function isDesignerPackage() {
    return String((stateRef.draft.manifest && stateRef.draft.manifest.layoutMode) || "").toLowerCase() === "designer";
  }

  function ensureElements() {
    if (!stateRef.draft.layout) stateRef.draft.layout = {};
    if (!stateRef.draft.layout.elements) stateRef.draft.layout.elements = [];
    if (!stateRef.draft.layout.page) stateRef.draft.layout.page = { size: "A4", marginMm: 2 };
  }

  /**
   * On this branch Studio is WYSIWYG-first: promote composition packs to designer
   * elements so opening clinical-01 never falls back to tree + PDF preview.
   */
  function promoteToDesignerIfNeeded() {
    ensureElements();
    const manifest = stateRef.draft.manifest || (stateRef.draft.manifest = {});
    const layout = stateRef.draft.layout;

    if (isDesignerPackage() && layout.elements.length > 0)
      return false;

    const body = layout.body || [];
    const hasAnnual = body.some((n) => n && n.widget === "clinical.hct-epo-annual-table");
    const hasCopay = body.some((n) => n && n.widget === "clinical.hct-epo-copay");
    const hasThaiHeader = layout.header && layout.header.widget === "thaiur.header";
    const isClinical01 =
      String(manifest.id || "").indexOf("clinical-01-hct-epo") === 0
      || String(manifest.dataAdapter || "") === "clinical-01-hct-epo"
      || hasAnnual;

    if (isClinical01 && layout.elements.length === 0) {
      layout.elements = [];
      if (hasThaiHeader || isClinical01) {
        layout.elements.push({
          id: "hdr",
          type: "header",
          preset: "thaiur-header-v1",
          box: { xMm: 0, yMm: 0, wMm: 206, hMm: 27 },
        });
      }
      layout.elements.push({
        id: "annual",
        type: "config-table",
        presetId: "hct-epo-annual-v1",
        box: { xMm: 0, yMm: 29, wMm: 206, hMm: hasCopay || isClinical01 ? 228 : 250 },
        bindings: CLINICAL01_ANNUAL_BINDINGS.slice(),
      });
      if (hasCopay || isClinical01) {
        layout.elements.push({
          id: "copay",
          type: "dense",
          widget: "clinical.hct-epo-copay",
          box: { xMm: 0, yMm: 259, wMm: 206, hMm: 34 },
          chrome: { headerFill: "$branding.sectionHeaderBackground", border: "thin" },
        });
      }
      layout.page = layout.page || { size: "A4", orientation: "portrait", marginMm: 2 };
      if (layout.page.marginMm == null) layout.page.marginMm = 2;
    }

    if (layout.elements.length === 0) {
      layout.elements.push({
        id: "tbl_main",
        type: "config-table",
        presetId: "hct-epo-annual-v1",
        box: { xMm: 0, yMm: 10, wMm: 206, hMm: 250 },
        bindings: [],
      });
    }

    manifest.layoutMode = "designer";
    // Keep body/header for JSON reference but Studio edits elements only.
    return true;
  }

  function mmScale(page) {
    const landscape = String((page && page.orientation) || "").toLowerCase() === "landscape";
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
    // Prefer clinical-01 sample when designer pack has no sample.
    const sampleId = item.id;
    const q = qs.toString() ? "?" + qs.toString() : "";
    try {
      sampleData = await apiRef(`/api/hprp/packages/${encodeURIComponent(sampleId)}/sample-data${q}`);
    } catch (_) {
      if (String(sampleId).indexOf("clinical-01") === 0 && sampleId !== "clinical-01-hct-epo") {
        try {
          sampleData = await apiRef(`/api/hprp/packages/clinical-01-hct-epo/sample-data${q}`);
        } catch (__) {
          sampleData = null;
        }
      } else {
        sampleData = null;
      }
    }
  }

  function renderCanvas() {
    const host = document.getElementById("designerCanvas");
    if (!host || !isStudioCanvas()) return;
    ensureElements();
    host.innerHTML = "";
    const page = stateRef.draft.layout.page || { size: "A4" };
    const { h, scale, landscape } = mmScale(page);
    const sheet = document.createElement("div");
    sheet.className = "designer-sheet" + (landscape ? " landscape" : "");
    sheet.style.width = DISPLAY_W + "px";
    sheet.style.height = h * scale + "px";
    sheet.addEventListener("click", () => {
      selectedElementId = null;
      renderInspector();
      renderCanvas();
    });

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
          wrap.appendChild(renderTableHtml(model));
        } else {
          wrap.innerHTML = `<div class="ph-dense">config-table · โหลด preset…</div>`;
        }
      } else if (el.type === "header") {
        const patient = sampleData && sampleData.header && sampleData.header.patient;
        const title = (sampleData && sampleData.title) || "Header";
        wrap.classList.add("designer-header-placeholder");
        wrap.innerHTML =
          `<div class="ph-title">${escapeHtml(title)}</div>` +
          `<div class="ph-meta">${escapeHtml((patient && patient.name) || "Patient")} · HN ${escapeHtml((patient && patient.hn) || "—")}</div>`;
      } else {
        wrap.innerHTML = `<div class="ph-dense">${escapeHtml(el.type)}: ${escapeHtml(el.widget || el.id)}</div>`;
      }

      sheet.appendChild(wrap);
    });

    host.appendChild(sheet);
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function renderTableHtml(model) {
    const root = document.createElement("div");
    root.className = "cfg-table";
    const p = model.preset;
    const table = document.createElement("table");
    table.cellSpacing = 0;
    table.cellPadding = 0;

    const thead = document.createElement("thead");
    const hr = document.createElement("tr");
    model.headerLabels.forEach((text) => {
      const th = document.createElement("th");
      th.textContent = text;
      th.addEventListener("click", (e) => {
        e.stopPropagation();
        // keep parent element selected; inspector shows columns
      });
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
    if (!insp || !isStudioCanvas()) return;
    insp.innerHTML = "";

    if (stateRef.selectedKey === "page") {
      renderPageInspector(insp);
      return;
    }
    if (stateRef.selectedKey === "labels") {
      insp.innerHTML = "<p class=\"muted\">ใช้แท็บ JSON → labels สำหรับแก้ข้อความคอลัมน์แบบละเอียด หรือแก้ labelKey ในคอลัมน์ด้านล่าง</p>";
      return;
    }

    const el = selectedElement();
    if (!el) {
      insp.innerHTML = "<p class=\"muted\">คลิก element บน canvas (ตาราง / header) เพื่อแก้</p>";
      const hint = document.createElement("p");
      hint.className = "muted";
      hint.textContent = "หรือกด + Table เพื่อเพิ่มตารางใหม่";
      insp.appendChild(hint);
      return;
    }

    insp.appendChild(Object.assign(document.createElement("p"), {
      innerHTML: `<strong>${escapeHtml(el.type)}</strong> <span class="muted">${escapeHtml(el.id)}</span>`,
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
    slotsLabel.textContent = "Slots per group (แถวต่อเดือน)";
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
    colHead.innerHTML = "<strong>Columns</strong> <span class=\"muted\">+/− อัปเดต canvas ทันที</span>";
    insp.appendChild(colHead);

    (working.columns || []).forEach((col, idx) => {
      const row = document.createElement("div");
      row.className = "col-row";
      const name = document.createElement("input");
      name.type = "text";
      name.value = col.id;
      name.title = "column id";
      name.addEventListener("change", () => {
        col.id = name.value.trim() || col.id;
        commitWorking(el, working);
        renderAll();
      });
      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "−";
      del.title = "Remove column";
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
    renderBoxFields(insp, el);
  }

  function ensureWorkingPreset(el, preset) {
    if (el.tablePreset) return el.tablePreset;
    // Clone so +/- mutates a local copy then detaches automatically.
    el.tablePreset = JSON.parse(JSON.stringify(preset));
    if (!el.tablePreset.id) el.tablePreset.id = el.presetId || "inline-table";
    delete el.presetId;
    return el.tablePreset;
  }

  function commitWorking(el, working) {
    el.tablePreset = working;
    delete el.presetId;
    stateRef.draft.manifest.layoutMode = "designer";
  }

  function renderPageInspector(insp) {
    ensureElements();
    const page = stateRef.draft.layout.page;
    ["size", "orientation"].forEach((key) => {
      const lab = document.createElement("label");
      lab.textContent = key;
      const input = document.createElement("input");
      input.type = "text";
      input.value = String(page[key] || (key === "size" ? "A4" : "portrait"));
      input.addEventListener("change", () => {
        page[key] = input.value;
        renderAll();
      });
      lab.appendChild(input);
      insp.appendChild(lab);
    });
    const m = document.createElement("label");
    m.textContent = "marginMm";
    const mi = document.createElement("input");
    mi.type = "number";
    mi.value = String(page.marginMm != null ? page.marginMm : 2);
    mi.addEventListener("change", () => {
      page.marginMm = Number(mi.value);
      renderAll();
    });
    m.appendChild(mi);
    insp.appendChild(m);
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
    } else {
      const note = document.createElement("p");
      note.className = "muted";
      note.textContent = "ไม่มี adapter schema — ตั้ง dataAdapter ใน manifest";
      insp.appendChild(note);
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

  function addConfigTable() {
    ensureElements();
    promoteToDesignerIfNeeded();
    const id = "tbl_" + Math.random().toString(36).slice(2, 7);
    stateRef.draft.layout.elements.push({
      id,
      type: "config-table",
      presetId: "hct-epo-annual-v1",
      box: { xMm: 0, yMm: 30, wMm: 206, hMm: 200 },
      bindings: [],
    });
    stateRef.draft.manifest.layoutMode = "designer";
    selectedElementId = id;
    stateRef.selectedKey = null;
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
      setStatusRef("Canvas WYSIWYG — คลิกตารางเพื่อแก้คอลัมน์ / row mode / mapping", "ok");
    }
  }

  // Compatibility alias: older studio.js checks TableDesigner.isDesignerMode()
  function isDesignerMode() {
    return isStudioCanvas();
  }

  global.TableDesigner = {
    isDesignerMode,
    isStudioCanvas,
    syncBodyClass,
    renderAll,
    onPackageOpened,
    ensureElements,
    promoteToDesignerIfNeeded,
    addConfigTable,
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
