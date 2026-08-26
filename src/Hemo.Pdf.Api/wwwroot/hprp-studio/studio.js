const state = {
  list: [],
  selected: null,
  tab: "manifest",
  mode: "designer",
  draft: { manifest: {}, layout: { body: [] }, labels: {} },
  catalog: { widgets: [], blockTypes: [], sampleTemplateIds: [] },
  selectedKey: null,
  previewUrl: null,
  previewTimer: null,
  sampleScenario: "",
};

const DIALYSIS_WIDGET = "hemosheet.dialysis-records";
const DENSE_HEMOSHEET_KINDS = new Set(["DefaultForm", "ThaiUrForm"]);

const els = {
  token: document.getElementById("token"),
  tenant: document.getElementById("tenant"),
  list: document.getElementById("packageList"),
  editor: document.getElementById("jsonEditor"),
  title: document.getElementById("editorTitle"),
  status: document.getElementById("status"),
  palette: document.getElementById("palette"),
  bodyList: document.getElementById("bodyList"),
  inspector: document.getElementById("inspector"),
  preview: document.getElementById("previewFrame"),
  btnPreview: document.getElementById("btnPreview"),
  sampleScenario: document.getElementById("sampleScenario"),
  previewEntityId: document.getElementById("previewEntityId"),
};

function headers() {
  const token = els.token.value.trim() || "dev";
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
    "X-Tenant-Code": els.tenant.value.trim() || "local",
  };
}

function setStatus(text, kind) {
  els.status.textContent = text;
  els.status.className = "status" + (kind ? ` ${kind}` : "");
}

async function api(path, options) {
  const res = await fetch(path, { ...options, headers: { ...headers(), ...(options && options.headers) } });
  const text = await res.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = { raw: text }; }
  if (!res.ok) {
    const errors = body && body.errors ? body.errors.join("\n") : (body && body.raw) || text || res.statusText;
    throw new Error(errors);
  }
  return body;
}

function pretty(value) {
  return JSON.stringify(value ?? {}, null, 2);
}

function keyOf(item) {
  return `${item.id}#${item.variant || "default"}`;
}

function ensureLayout() {
  if (!state.draft.layout || typeof state.draft.layout !== "object")
    state.draft.layout = {};
  if (!Array.isArray(state.draft.layout.body))
    state.draft.layout.body = [];
  if (!Array.isArray(state.draft.layout.sections))
    state.draft.layout.sections = [];
  if (!state.draft.labels || typeof state.draft.labels !== "object")
    state.draft.labels = {};
}

/** Hemosheet packages edit layout.sections[]; clinical-01 style edits body[]. */
function usesSectionsMode() {
  ensureLayout();
  if ((state.draft.layout.sections || []).length > 0)
    return true;
  const id = (state.selected && state.selected.id) || "";
  return String(id).toLowerCase().includes("hemodialysis-record")
    || String(id).toLowerCase() === "clinical-03-hemodialysis-record"
    || String(id).toLowerCase() === "template-04-hemosheet";
}

function manifestLayoutKind() {
  return state.draft.manifest && state.draft.manifest.layoutKind;
}

function isDenseHemosheetForm() {
  return usesSectionsMode() && DENSE_HEMOSHEET_KINDS.has(manifestLayoutKind() || "");
}

function profileLabel(item) {
  if (!item) return "default";
  return item.profileLabel || item.layoutProfile || item.variant || "default";
}

function findDialysisSectionKey() {
  ensureLayout();
  const idx = state.draft.layout.sections.findIndex((n) => n && n.widget === DIALYSIS_WIDGET);
  return idx >= 0 ? "sections:" + idx : null;
}

function listSlot() {
  return usesSectionsMode() ? "sections" : "body";
}

function nodeList() {
  ensureLayout();
  return usesSectionsMode() ? state.draft.layout.sections : state.draft.layout.body;
}

function labelLang() {
  const fromManifest = state.draft.manifest && state.draft.manifest.language;
  if (fromManifest) return fromManifest;
  const keys = Object.keys(state.draft.labels || {});
  return keys[0] || "th";
}

function labelMap() {
  const lang = labelLang();
  if (!state.draft.labels[lang] || typeof state.draft.labels[lang] !== "object")
    state.draft.labels[lang] = {};
  return state.draft.labels[lang];
}

function recipeFor(node) {
  if (!node) return null;
  if (node.widget)
    return (state.catalog.widgets || []).find((w) => w.id === node.widget) || null;
  if (node.type)
    return (state.catalog.blockTypes || []).find((b) => b.id === node.type) || null;
  return null;
}

function allowedOn(recipe, templateId) {
  if (!recipe) return false;
  if (!recipe.allowedOn || recipe.allowedOn.length === 0) return true;
  const id = (templateId || "").toLowerCase();
  return recipe.allowedOn.some((x) => String(x).toLowerCase() === id);
}

function nodeAt(key) {
  ensureLayout();
  if (key === "header") return state.draft.layout.header || null;
  if (key && key.startsWith("body:")) {
    const i = Number(key.slice(5));
    return state.draft.layout.body[i] || null;
  }
  if (key && key.startsWith("sections:")) {
    const i = Number(key.slice(9));
    return state.draft.layout.sections[i] || null;
  }
  return null;
}

function setNodeAt(key, node) {
  ensureLayout();
  if (key === "header") {
    state.draft.layout.header = node;
    return;
  }
  if (key && key.startsWith("body:")) {
    const i = Number(key.slice(5));
    if (node == null)
      state.draft.layout.body.splice(i, 1);
    else
      state.draft.layout.body[i] = node;
    return;
  }
  if (key && key.startsWith("sections:")) {
    const i = Number(key.slice(9));
    if (node == null)
      state.draft.layout.sections.splice(i, 1);
    else
      state.draft.layout.sections[i] = node;
  }
}

function nodeTitle(node) {
  if (!node) return "(empty)";
  if (node.widget) return node.widget;
  if (node.type) return node.type;
  return "(untitled)";
}

function flushJsonEditor() {
  if (state.mode !== "json" || !state.selected) return;
  state.draft[state.tab] = JSON.parse(els.editor.value);
  ensureLayout();
}

function showJsonTab() {
  els.editor.value = pretty(state.draft[state.tab]);
}

function setMode(mode) {
  if (mode === "designer" && state.mode === "json") {
    try { flushJsonEditor(); } catch (err) {
      setStatus("Fix JSON before leaving advanced mode: " + err.message, "err");
      return;
    }
  }
  state.mode = mode;
  document.body.classList.toggle("mode-designer", mode === "designer");
  document.body.classList.toggle("mode-json", mode === "json");
  document.getElementById("btnModeDesigner").classList.toggle("active", mode === "designer");
  document.getElementById("btnModeJson").classList.toggle("active", mode === "json");
  if (mode === "json")
    showJsonTab();
  else {
    renderDesigner();
    schedulePreview();
  }
}

function selectPackage(item) {
  state.selected = item;
  [...els.list.children].forEach((li) => {
    li.classList.toggle("active", li.dataset.key === keyOf(item));
  });
}

function setButtonsEnabled(on) {
  ["btnValidate", "btnSave", "btnPackThis", "btnValidateJson", "btnSaveJson", "btnPackThisJson", "btnPreview"]
    .forEach((id) => {
      const el = document.getElementById(id);
      if (el) el.disabled = !on;
    });
  els.editor.disabled = !on;
}

async function loadList() {
  els.list.innerHTML = `<li class="muted">Loading…</li>`;
  const items = await api("/api/hprp/packages");
  state.list = items || [];
  els.list.innerHTML = "";
  if (!state.list.length) {
    els.list.innerHTML = `<li class="muted">No packages found</li>`;
    return;
  }
  for (const item of state.list) {
    const li = document.createElement("li");
    li.dataset.key = keyOf(item);
    const label = profileLabel(item);
    li.innerHTML = `<span class="id">${item.displayName || item.id}</span><span class="meta">${label}${item.variant ? " · " + item.variant : ""} · ${item.packed ? "packed" : "folder"}</span>`;
    li.addEventListener("click", () => openPackage(item).catch((err) => setStatus(err.message, "err")));
    els.list.appendChild(li);
  }
}

async function loadCatalog() {
  state.catalog = await api("/api/hprp/catalog");
}

function renderPalette() {
  const templateId = state.selected && state.selected.id;
  const slot = listSlot();
  const widgets = (state.catalog.widgets || []).filter((w) => {
    if (!allowedOn(w, templateId)) return false;
    const recipeSlot = w.slot || "body";
    return recipeSlot === slot;
  });
  els.palette.innerHTML = "";
  const addGroup = (title) => {
    const g = document.createElement("div");
    g.className = "group";
    g.textContent = title;
    els.palette.appendChild(g);
  };
  addGroup("Widgets");
  for (const recipe of widgets) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.textContent = recipe.id;
    btn.title = recipe.kind + (recipe.allowedOn && recipe.allowedOn.length ? " · " + recipe.allowedOn.join(", ") : "");
    btn.addEventListener("click", () => addNode(newWidgetNode(recipe)));
    els.palette.appendChild(btn);
  }
  if (slot === "body") {
    addGroup("Blocks");
    for (const recipe of state.catalog.blockTypes || []) {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = recipe.id;
      btn.addEventListener("click", () => addNode(newBlock(recipe.id)));
      els.palette.appendChild(btn);
    }
  }
}

function newWidgetNode(recipe) {
  const node = { widget: recipe.id };
  if (recipe.defaultColumns && recipe.defaultColumns.length)
    node.columns = recipe.defaultColumns.slice();
  if (recipe.defaultColumnsWhen && typeof recipe.defaultColumnsWhen === "object")
    node.columnsWhen = JSON.parse(JSON.stringify(recipe.defaultColumnsWhen));
  if (recipe.chromeDefaults && recipe.chromeDefaults.headerFill)
    node.chrome = { headerFill: recipe.chromeDefaults.headerFill };
  return node;
}

function newBlock(type) {
  if (type === "key-value-table")
    return { type, rows: [{ label: "", content: "" }] };
  if (type === "field-grid")
    return { type, columns: 2, fields: [] };
  if (type === "text")
    return { type, content: "" };
  return { type };
}

function addNode(node) {
  if (!state.selected) return;
  ensureLayout();
  if (!usesSectionsMode() && node.widget === "thaiur.header" && !state.draft.layout.header) {
    state.draft.layout.header = node;
    state.selectedKey = "header";
  } else if (usesSectionsMode()) {
    state.draft.layout.sections.push(node);
    state.selectedKey = "sections:" + (state.draft.layout.sections.length - 1);
  } else {
    state.draft.layout.body.push(node);
    state.selectedKey = "body:" + (state.draft.layout.body.length - 1);
  }
  renderDesigner();
  schedulePreview();
}

function moveListItem(index, delta) {
  ensureLayout();
  const next = index + delta;
  const list = nodeList();
  if (next < 0 || next >= list.length) return;
  const [item] = list.splice(index, 1);
  list.splice(next, 0, item);
  state.selectedKey = listSlot() + ":" + next;
  renderDesigner();
  schedulePreview();
}

function removeNode(key) {
  if (key === "header") {
    state.draft.layout.header = undefined;
    if (state.selectedKey === "header") state.selectedKey = null;
  } else if (key.startsWith("body:")) {
    const i = Number(key.slice(5));
    state.draft.layout.body.splice(i, 1);
    state.selectedKey = state.draft.layout.body.length
      ? "body:" + Math.min(i, state.draft.layout.body.length - 1)
      : null;
  } else if (key.startsWith("sections:")) {
    const i = Number(key.slice(9));
    state.draft.layout.sections.splice(i, 1);
    state.selectedKey = state.draft.layout.sections.length
      ? "sections:" + Math.min(i, state.draft.layout.sections.length - 1)
      : null;
  }
  renderDesigner();
  schedulePreview();
}

function renderBodyList() {
  ensureLayout();
  els.bodyList.innerHTML = "";
  const titleEl = document.getElementById("orderListTitle");
  const slot = listSlot();
  if (titleEl) {
    const variant = state.selected && state.selected.variant ? state.selected.variant : "default";
    titleEl.textContent = slot === "sections"
      ? `Sections order · ${variant}`
      : "Body order";
  }
  if (slot === "body") {
    const header = state.draft.layout.header;
    if (header)
      els.bodyList.appendChild(bodyItem("header", header, { header: true }));
  }
  const list = nodeList();
  list.forEach((node, i) => {
    els.bodyList.appendChild(bodyItem(slot + ":" + i, node, {
      index: i,
      count: list.length,
      slot,
    }));
  });
}

function bodyItem(key, node, opts) {
  const li = document.createElement("li");
  li.className = key === state.selectedKey ? "active" : "";
  if (opts.header) li.classList.add("header-node");
  if (isDenseHemosheetForm() && node && node.widget === DIALYSIS_WIDGET)
    li.classList.add("preview-affects");
  const slotLabel = opts.header ? "header" : ((opts.slot || "body") + "[" + opts.index + "]");
  li.innerHTML = `<span class="id">${nodeTitle(node)}</span><span class="meta">${slotLabel}</span>`;
  if (!opts.header) {
    li.draggable = true;
    li.addEventListener("dragstart", () => { state.dragIndex = opts.index; });
    li.addEventListener("dragover", (e) => e.preventDefault());
    li.addEventListener("drop", (e) => {
      e.preventDefault();
      if (state.dragIndex == null || state.dragIndex === opts.index) return;
      moveListItem(state.dragIndex, opts.index - state.dragIndex);
      state.dragIndex = null;
    });
  }
  li.addEventListener("click", () => {
    state.selectedKey = key;
    renderDesigner();
  });
  const actions = document.createElement("div");
  actions.className = "node-actions";
  if (!opts.header) {
    const up = document.createElement("button");
    up.type = "button";
    up.textContent = "Up";
    up.disabled = opts.index === 0;
    up.addEventListener("click", (e) => { e.stopPropagation(); moveListItem(opts.index, -1); });
    const down = document.createElement("button");
    down.type = "button";
    down.textContent = "Down";
    down.disabled = opts.index === opts.count - 1;
    down.addEventListener("click", (e) => { e.stopPropagation(); moveListItem(opts.index, 1); });
    actions.append(up, down);
  }
  const del = document.createElement("button");
  del.type = "button";
  del.textContent = "Remove";
  del.addEventListener("click", (e) => { e.stopPropagation(); removeNode(key); });
  actions.append(del);
  li.appendChild(actions);
  return li;
}

function field(label, input) {
  const wrap = document.createElement("label");
  wrap.textContent = label;
  wrap.appendChild(input);
  return wrap;
}

function textInput(value, onChange) {
  const input = document.createElement("input");
  input.type = "text";
  input.value = value == null ? "" : String(value);
  input.addEventListener("change", () => onChange(input.value));
  return input;
}

function numberInput(value, onChange) {
  const input = document.createElement("input");
  input.type = "number";
  input.step = "0.1";
  input.value = value == null ? "" : String(value);
  input.addEventListener("change", () => {
    const n = Number(input.value);
    onChange(Number.isFinite(n) && input.value !== "" ? n : null);
  });
  return input;
}

function selectInput(value, options, onChange) {
  const sel = document.createElement("select");
  for (const opt of options) {
    const o = document.createElement("option");
    o.value = opt.value;
    o.textContent = opt.label;
    sel.appendChild(o);
  }
  sel.value = value == null ? "" : String(value);
  sel.addEventListener("change", () => onChange(sel.value));
  return sel;
}

function checkboxRow(label, checked, onChange) {
  const row = document.createElement("div");
  row.className = "row-inline";
  const input = document.createElement("input");
  input.type = "checkbox";
  input.checked = !!checked;
  input.addEventListener("change", () => onChange(input.checked));
  const span = document.createElement("span");
  span.textContent = label;
  row.append(input, span);
  return row;
}

function mutateSelected(mutator) {
  const node = nodeAt(state.selectedKey);
  if (!node) return;
  mutator(node);
  setNodeAt(state.selectedKey, node);
  renderDesigner();
  schedulePreview();
}

function workingColumnPlan(node, recipe) {
  if (node.columnPlan && node.columnPlan.length)
    return node.columnPlan.map((c) => ({ ...c }));
  return (recipe.defaultColumnPlan || []).map((c) => ({ ...c }));
}

function plansEqual(a, b) {
  if (!a || !b || a.length !== b.length) return false;
  return a.every((c, i) =>
    (c.bind || "") === (b[i].bind || "")
    && (c.labelKey || "") === (b[i].labelKey || "")
    && Number(c.weight || 0) === Number(b[i].weight || 0)
    && !!c.center === !!b[i].center
    && !!c.isLab === !!b[i].isLab);
}

function persistColumnPlan(node, recipe, plan) {
  if (plansEqual(plan, recipe.defaultColumnPlan || []))
    delete node.columnPlan;
  else
    node.columnPlan = plan;
}

function workingStringColumns(node, recipe) {
  if (Array.isArray(node.columns) && node.columns.length)
    return node.columns.map((c) => String(c));
  return ((recipe && recipe.defaultColumns) || []).map((c) => String(c));
}

function stringColsEqual(a, b) {
  if (!a || !b || a.length !== b.length) return false;
  return a.every((c, i) => String(c) === String(b[i]));
}

function persistStringColumns(node, recipe, cols) {
  if (stringColsEqual(cols, (recipe && recipe.defaultColumns) || []))
    delete node.columns;
  else
    node.columns = cols.slice();
}

function renderInspector() {
  const node = nodeAt(state.selectedKey);
  els.inspector.innerHTML = "";
  if (!node) {
    els.inspector.innerHTML = `<p class="muted">เลือกบล็อกในรายการ</p>`;
    return;
  }
  const recipe = recipeFor(node);
  const head = document.createElement("p");
  head.innerHTML = `<strong>${nodeTitle(node)}</strong><br/><span class="muted">${recipe ? recipe.kind : "custom"}</span>`;
  els.inspector.appendChild(head);

  if (isDenseHemosheetForm() && node.widget !== DIALYSIS_WIDGET) {
    const note = document.createElement("p");
    note.className = "inspector-note";
    note.textContent = "แบบ " + (manifestLayoutKind() || "dense") + ": บล็อกนี้ยังไม่ขับ preview (ลำดับ/when/chrome ไม่มีผล) — แก้ hemosheet.dialysis-records เพื่อดูผลใน PDF";
    els.inspector.appendChild(note);
  }

  const fields = (recipe && recipe.inspectorFields) || [];
  if (fields.includes("when")) {
    const whenVal = Array.isArray(node.when) ? node.when.join(",") : (node.when || "");
    els.inspector.appendChild(field("when", textInput(whenVal, (v) => mutateSelected((n) => {
      const trimmed = v.trim();
      if (!trimmed) delete n.when;
      else if (trimmed.includes(",")) n.when = trimmed.split(",").map((s) => s.trim()).filter(Boolean);
      else n.when = trimmed;
    }))));
  }

  if (fields.includes("variant")) {
    els.inspector.appendChild(field("variant", textInput(node.variant || "", (v) => mutateSelected((n) => {
      if (v.trim()) n.variant = v.trim();
      else delete n.variant;
    }))));
  }

  if (fields.includes("fixedLinesFrom")) {
    els.inspector.appendChild(field("fixedLinesFrom", textInput(node.fixedLinesFrom || "", (v) => mutateSelected((n) => {
      if (v.trim()) n.fixedLinesFrom = v.trim();
      else delete n.fixedLinesFrom;
    }))));
  }

  if (fields.some((f) => f.startsWith("chrome"))) {
    const chrome = node.chrome || {};
    const clearChromeIfEmpty = (n) => {
      const c = n.chrome || {};
      const empty = !c.headerFill && !c.border && c.fontSize == null
        && c.rowHeightMm == null
        && !(c.columnWidths && c.columnWidths.length)
        && !(c.bandWeights && c.bandWeights.length);
      if (empty) delete n.chrome;
    };
    const fillInput = textInput(chrome.headerFill || "", (v) => mutateSelected((n) => {
      n.chrome = { ...(n.chrome || {}), headerFill: v.trim() || undefined };
      clearChromeIfEmpty(n);
    }));
    fillInput.placeholder = "$branding.sectionHeaderBackground หรือ #384BA8";
    els.inspector.appendChild(field("chrome.headerFill", fillInput));
    els.inspector.appendChild(field("chrome.border", selectInput(chrome.border || "", [
      { value: "", label: "(default thin)" },
      { value: "none", label: "none" },
      { value: "thin", label: "thin" },
      { value: "medium", label: "medium" },
    ], (v) => mutateSelected((n) => {
      n.chrome = { ...(n.chrome || {}), border: v || undefined };
      clearChromeIfEmpty(n);
    }))));
    els.inspector.appendChild(field("chrome.fontSize", numberInput(chrome.fontSize, (v) => mutateSelected((n) => {
      n.chrome = { ...(n.chrome || {}), fontSize: v };
      clearChromeIfEmpty(n);
    }))));

    if (fields.includes("chrome.rowHeightMm")) {
      const rowH = numberInput(chrome.rowHeightMm, (v) => mutateSelected((n) => {
        n.chrome = { ...(n.chrome || {}), rowHeightMm: v };
        clearChromeIfEmpty(n);
      }));
      rowH.placeholder = "ว่าง = budget อัตโนมัติ (mm)";
      els.inspector.appendChild(field("chrome.rowHeightMm", rowH));
    }

    if (fields.includes("chrome.columnWidths")) {
      const widthsVal = Array.isArray(chrome.columnWidths) ? chrome.columnWidths.join(",") : "";
      const widthsInput = textInput(widthsVal, (v) => mutateSelected((n) => {
        const parts = v.split(",").map((s) => s.trim()).filter(Boolean);
        n.chrome = { ...(n.chrome || {}), columnWidths: parts.length ? parts : undefined };
        clearChromeIfEmpty(n);
      }));
      widthsInput.placeholder = "เช่น 18mm,2.4,1.1,1.1";
      els.inspector.appendChild(field("chrome.columnWidths", widthsInput));
    }

    if (fields.includes("chrome.bandWeights")) {
      const bandsVal = Array.isArray(chrome.bandWeights) ? chrome.bandWeights.join(",") : "";
      const bandsInput = textInput(bandsVal, (v) => mutateSelected((n) => {
        const parts = v.split(",").map((s) => s.trim()).filter(Boolean).map(Number).filter((x) => Number.isFinite(x) && x > 0);
        n.chrome = { ...(n.chrome || {}), bandWeights: parts.length ? parts : undefined };
        clearChromeIfEmpty(n);
      }));
      bandsInput.placeholder = "S,O,A,P เช่น 1,2.5,1,1";
      els.inspector.appendChild(field("chrome.bandWeights", bandsInput));
    }
  }

  if (fields.includes("columnPlan") && recipe) {
    const plan = workingColumnPlan(node, recipe);
    const title = document.createElement("p");
    title.innerHTML = "<strong>columnPlan</strong>";
    els.inspector.appendChild(title);
    plan.forEach((col, i) => {
      const card = document.createElement("div");
      card.className = "col-card";
      card.appendChild(field("bind", selectInput(col.bind || "", [
        { value: "", label: "(blank handwriting)" },
        ...(recipe.bindFields || []).map((b) => ({ value: b.bind, label: b.bind })),
      ], (v) => {
        plan[i].bind = v;
        const known = (recipe.bindFields || []).find((b) => b.bind === v);
        if (known && known.labelKey) plan[i].labelKey = known.labelKey;
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      })));
      card.appendChild(field("labelKey", textInput(col.labelKey || "", (v) => {
        plan[i].labelKey = v.trim();
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      })));
      card.appendChild(field("weight", numberInput(col.weight, (v) => {
        plan[i].weight = v;
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      })));
      card.appendChild(checkboxRow("center", col.center, (v) => {
        plan[i].center = v;
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      }));
      card.appendChild(checkboxRow("isLab", col.isLab, (v) => {
        plan[i].isLab = v;
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      }));
      const nav = document.createElement("div");
      nav.className = "node-actions";
      const up = document.createElement("button");
      up.type = "button";
      up.textContent = "Up";
      up.disabled = i === 0;
      up.addEventListener("click", () => {
        if (i === 0) return;
        [plan[i - 1], plan[i]] = [plan[i], plan[i - 1]];
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      });
      const down = document.createElement("button");
      down.type = "button";
      down.textContent = "Down";
      down.disabled = i === plan.length - 1;
      down.addEventListener("click", () => {
        if (i === plan.length - 1) return;
        [plan[i], plan[i + 1]] = [plan[i + 1], plan[i]];
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      });
      const hide = document.createElement("button");
      hide.type = "button";
      hide.textContent = "Remove column";
      hide.addEventListener("click", () => {
        plan.splice(i, 1);
        mutateSelected((n) => persistColumnPlan(n, recipe, plan));
      });
      nav.append(up, down, hide);
      card.appendChild(nav);
      els.inspector.appendChild(card);
    });
    const add = document.createElement("button");
    add.type = "button";
    add.textContent = "Add blank column";
    add.addEventListener("click", () => {
      plan.push({ bind: "", labelKey: "", weight: 1, center: false, isLab: false });
      mutateSelected((n) => persistColumnPlan(n, recipe, plan));
    });
    els.inspector.appendChild(add);
  }

  if (fields.includes("columns") && !fields.includes("columnPlan")
      && recipe && (recipe.slot === "sections" || (recipe.defaultColumns || []).length || Array.isArray(node.columns))) {
    const cols = workingStringColumns(node, recipe);
    const title = document.createElement("p");
    title.innerHTML = "<strong>columns</strong>";
    els.inspector.appendChild(title);
    cols.forEach((label, i) => {
      const card = document.createElement("div");
      card.className = "col-card";
      card.appendChild(field("label", textInput(label, (v) => {
        cols[i] = v;
        mutateSelected((n) => persistStringColumns(n, recipe, cols));
      })));
      const nav = document.createElement("div");
      nav.className = "node-actions";
      const up = document.createElement("button");
      up.type = "button";
      up.textContent = "Up";
      up.disabled = i === 0;
      up.addEventListener("click", () => {
        if (i === 0) return;
        [cols[i - 1], cols[i]] = [cols[i], cols[i - 1]];
        mutateSelected((n) => persistStringColumns(n, recipe, cols));
      });
      const down = document.createElement("button");
      down.type = "button";
      down.textContent = "Down";
      down.disabled = i === cols.length - 1;
      down.addEventListener("click", () => {
        if (i === cols.length - 1) return;
        [cols[i], cols[i + 1]] = [cols[i + 1], cols[i]];
        mutateSelected((n) => persistStringColumns(n, recipe, cols));
      });
      const hide = document.createElement("button");
      hide.type = "button";
      hide.textContent = "Remove";
      hide.addEventListener("click", () => {
        cols.splice(i, 1);
        mutateSelected((n) => persistStringColumns(n, recipe, cols));
      });
      nav.append(up, down, hide);
      card.appendChild(nav);
      els.inspector.appendChild(card);
    });
    const addCol = document.createElement("button");
    addCol.type = "button";
    addCol.textContent = "Add column";
    addCol.addEventListener("click", () => {
      cols.push("");
      mutateSelected((n) => persistStringColumns(n, recipe, cols));
    });
    els.inspector.appendChild(addCol);
  }

  if (fields.includes("columnsWhen") && recipe) {
    const key = "feature:showHdfColumns";
    const whenMap = node.columnsWhen && typeof node.columnsWhen === "object"
      ? node.columnsWhen
      : (recipe.defaultColumnsWhen || {});
    const hdfCols = Array.isArray(whenMap[key])
      ? whenMap[key].slice()
      : ((recipe.defaultColumnsWhen && recipe.defaultColumnsWhen[key]) || []).slice();
    const title = document.createElement("p");
    title.innerHTML = "<strong>columnsWhen." + key + "</strong>";
    els.inspector.appendChild(title);
    hdfCols.forEach((label, i) => {
      const card = document.createElement("div");
      card.className = "col-card";
      card.appendChild(field("label", textInput(label, (v) => {
        hdfCols[i] = v;
        mutateSelected((n) => {
          n.columnsWhen = { ...(n.columnsWhen || {}), [key]: hdfCols.slice() };
        });
      })));
      const nav = document.createElement("div");
      nav.className = "node-actions";
      const up = document.createElement("button");
      up.type = "button";
      up.textContent = "Up";
      up.disabled = i === 0;
      up.addEventListener("click", () => {
        if (i === 0) return;
        [hdfCols[i - 1], hdfCols[i]] = [hdfCols[i], hdfCols[i - 1]];
        mutateSelected((n) => {
          n.columnsWhen = { ...(n.columnsWhen || {}), [key]: hdfCols.slice() };
        });
      });
      const down = document.createElement("button");
      down.type = "button";
      down.textContent = "Down";
      down.disabled = i === hdfCols.length - 1;
      down.addEventListener("click", () => {
        if (i === hdfCols.length - 1) return;
        [hdfCols[i], hdfCols[i + 1]] = [hdfCols[i + 1], hdfCols[i]];
        mutateSelected((n) => {
          n.columnsWhen = { ...(n.columnsWhen || {}), [key]: hdfCols.slice() };
        });
      });
      const hide = document.createElement("button");
      hide.type = "button";
      hide.textContent = "Remove";
      hide.addEventListener("click", () => {
        hdfCols.splice(i, 1);
        mutateSelected((n) => {
          n.columnsWhen = { ...(n.columnsWhen || {}), [key]: hdfCols.slice() };
        });
      });
      nav.append(up, down, hide);
      card.appendChild(nav);
      els.inspector.appendChild(card);
    });
    const addHdf = document.createElement("button");
    addHdf.type = "button";
    addHdf.textContent = "Add HDF column";
    addHdf.addEventListener("click", () => {
      hdfCols.push("");
      mutateSelected((n) => {
        n.columnsWhen = { ...(n.columnsWhen || {}), [key]: hdfCols.slice() };
      });
    });
    els.inspector.appendChild(addHdf);
  }

  if (fields.includes("rows")) {
    const rows = Array.isArray(node.rows) ? node.rows : [];
    const title = document.createElement("p");
    title.innerHTML = "<strong>rows</strong>";
    els.inspector.appendChild(title);
    rows.forEach((row, i) => {
      const card = document.createElement("div");
      card.className = "col-card";
      const labelVal = row && row.label && typeof row.label === "object" && row.label.$label
        ? "$" + row.label.$label
        : (row && (typeof row.label === "string" ? row.label : (row.label || "")));
      const contentVal = row && typeof row.content === "string" ? row.content : (row && row.content != null ? JSON.stringify(row.content) : "");
      card.appendChild(field("label ($key for $label)", textInput(labelVal, (v) => mutateSelected((n) => {
        const copy = [...(n.rows || [])];
        const next = { ...(copy[i] || {}) };
        if (v.startsWith("$") && v.length > 1) next.label = { $label: v.slice(1) };
        else next.label = v;
        copy[i] = next;
        n.rows = copy;
      }))));
      card.appendChild(field("content", textInput(contentVal, (v) => mutateSelected((n) => {
        const copy = [...(n.rows || [])];
        copy[i] = { ...(copy[i] || {}), content: v };
        n.rows = copy;
      }))));
      const del = document.createElement("button");
      del.type = "button";
      del.textContent = "Remove row";
      del.addEventListener("click", () => mutateSelected((n) => {
        n.rows = (n.rows || []).filter((_, idx) => idx !== i);
      }));
      card.appendChild(del);
      els.inspector.appendChild(card);
    });
    const addRow = document.createElement("button");
    addRow.type = "button";
    addRow.textContent = "Add row";
    addRow.addEventListener("click", () => mutateSelected((n) => {
      n.rows = [...(n.rows || []), { label: "", content: "" }];
    }));
    els.inspector.appendChild(addRow);
  }

  if (fields.includes("content")) {
    const content = typeof node.content === "string" ? node.content : "";
    els.inspector.appendChild(field("content", textInput(content, (v) => mutateSelected((n) => { n.content = v; }))));
  }

  if (fields.includes("title")) {
    const titleVal = typeof node.title === "string" ? node.title : "";
    els.inspector.appendChild(field("title", textInput(titleVal, (v) => mutateSelected((n) => { n.title = v; }))));
  }

  if (recipe && recipe.labelKeys && recipe.labelKeys.length) {
    const map = labelMap();
    const title = document.createElement("p");
    title.innerHTML = "<strong>labels." + labelLang() + "</strong>";
    els.inspector.appendChild(title);
    for (const key of recipe.labelKeys) {
      els.inspector.appendChild(field(key, textInput(map[key] || "", (v) => {
        const lang = labelLang();
        if (!state.draft.labels[lang]) state.draft.labels[lang] = {};
        if (v.trim()) state.draft.labels[lang][key] = v;
        else delete state.draft.labels[lang][key];
        renderDesigner();
        schedulePreview();
      })));
    }
  }
}

function renderDenseFormHint() {
  const el = document.getElementById("denseFormHint");
  if (!el) return;
  if (!isDenseHemosheetForm()) {
    el.classList.add("hidden");
    el.textContent = "";
    return;
  }
  const profile = state.draft.manifest.layoutProfile || profileLabel(state.selected);
  el.classList.remove("hidden");
  el.innerHTML = `<strong>Preview แบบ ${profile}</strong> — ตั้งค่า tenant (Thai UR) ไม่มีผลใน Studio ต้องเปิดแพ็กเกจ variant ที่ตรงกัน `
    + `(เช่น <code>thaiur</code> ไม่ใช่ <code>default</code>). `
    + `บน ${manifestLayoutKind()} แก้แล้วเห็นผลใน preview เฉพาะ <code>${DIALYSIS_WIDGET}</code> `
    + `(columns / columnsWhen / chrome.headerFill / fixedLinesFrom) และต้องคงจำนวนคอลัมน์ 12 (HD) หรือ 14 (HDF).<br>`
    + `พรีวิวใช้ renderer เดียวกับพิมพ์จริง — เลือก Sample (HD/HDF mock) หรือใส่ EntityId เพื่อดึง report-data จริงเหมือนแอป`;
}

function renderDesigner() {
  if (state.mode !== "designer") return;
  renderDenseFormHint();
  renderPalette();
  renderBodyList();
  renderInspector();
}

function currentBody() {
  if (state.mode === "json")
    flushJsonEditor();
  ensureLayout();
  return {
    manifest: state.draft.manifest,
    layout: state.draft.layout,
    labels: state.draft.labels,
  };
}

async function openPackage(item) {
  const query = item.variant ? `?variant=${encodeURIComponent(item.variant)}` : "";
  const pkg = await api(`/api/hprp/packages/${encodeURIComponent(item.id)}${query}`);
  state.draft = {
    manifest: pkg.manifest || {},
    layout: pkg.layout || { body: [] },
    labels: pkg.labels || {},
  };
  ensureLayout();
  if (usesSectionsMode()) {
    state.selectedKey = findDialysisSectionKey()
      || (state.draft.layout.sections[0] ? "sections:0" : null);
  } else if (state.draft.layout.header) {
    state.selectedKey = "header";
  } else if (state.draft.layout.body[0]) {
    state.selectedKey = "body:0";
  } else {
    state.selectedKey = null;
  }
  selectPackage(item);
  const label = profileLabel(item);
  const kind = item.layoutKind || state.draft.manifest.layoutKind || "";
  els.title.textContent = `${item.id} · ${label}${kind ? " (" + kind + ")" : ""}`;
  setButtonsEnabled(true);
  if (state.mode === "json") showJsonTab();
  else renderDesigner();
  setStatus("Loaded " + item.id, "ok");
  await loadSampleScenarios(item.id);
  schedulePreview();
}

async function loadSampleScenarios(templateId) {
  if (!els.sampleScenario) return;
  try {
    const rows = await api(`/api/hprp/packages/${encodeURIComponent(templateId)}/samples`);
    els.sampleScenario.innerHTML = "";
    for (const row of rows || []) {
      const opt = document.createElement("option");
      opt.value = row.scenario || "";
      opt.textContent = row.label || row.id || "default";
      els.sampleScenario.appendChild(opt);
    }
    if (!els.sampleScenario.options.length) {
      const opt = document.createElement("option");
      opt.value = "";
      opt.textContent = "Full HD mock";
      els.sampleScenario.appendChild(opt);
    }
    els.sampleScenario.value = state.sampleScenario || "";
  } catch {
    els.sampleScenario.innerHTML = `<option value="">Full HD mock</option>`;
  }
}

async function validate() {
  await api("/api/hprp/validate", { method: "POST", body: JSON.stringify(currentBody()) });
  setStatus("Valid package.", "ok");
}

function firstFileHeaderFill(layout) {
  const nodes = [...(layout && layout.body) || [], ...(layout && layout.sections) || []];
  for (const node of nodes) {
    const fill = node && node.chrome && node.chrome.headerFill;
    if (typeof fill === "string" && fill.trim() && !fill.trim().startsWith("$")) {
      return fill.trim();
    }
  }
  return null;
}

async function save() {
  const body = currentBody();
  const item = state.selected;
  const query = item.variant ? `?variant=${encodeURIComponent(item.variant)}` : "";
  const result = await api(`/api/hprp/packages/${encodeURIComponent(item.id)}${query}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
  const packed = await api(`/api/hprp/packages/${encodeURIComponent(item.id)}${query}`);
  state.draft = {
    manifest: packed.manifest || {},
    layout: packed.layout || {},
    labels: packed.labels || {},
  };
  ensureLayout();
  if (state.mode === "json") showJsonTab();
  else renderDesigner();
  const fill = firstFileHeaderFill(state.draft.layout);
  setStatus(
    "Packed " + result.outputPath + (fill ? " · headerFill " + fill : " · headerFill uses tenant branding"),
    "ok"
  );
  await loadList();
  schedulePreview();
}

async function packAll() {
  const result = await api("/api/hprp/pack-from-templates", { method: "POST" });
  const count = Array.isArray(result) ? result.length : 0;
  await loadList();
  if (state.selected)
    await openPackage(state.selected);
  setStatus(`Packed ${count} package(s) from assets/templates/reports.`, "ok");
}

async function packSelectedFromDisk() {
  const item = state.selected;
  if (!item)
    throw new Error("Select a package first.");

  const result = await api(
    `/api/hprp/pack-from-templates/${encodeURIComponent(item.id)}`,
    { method: "POST" }
  );
  const files = Array.isArray(result)
    ? result.map((r) => r.outputPath || r.OutputPath).filter(Boolean)
    : [];
  await loadList();
  await openPackage(item);
  setStatus(
    files.length
      ? `Packed from disk:\n${files.join("\n")}`
      : `Packed ${item.id} from assets/templates/reports.`,
    "ok"
  );
}

function schedulePreview() {
  if (!state.selected) return;
  clearTimeout(state.previewTimer);
  state.previewTimer = setTimeout(() => {
    preview().catch((err) => setStatus(err.message, "err"));
  }, 700);
}

async function preview() {
  const item = state.selected;
  if (!item) return;
  const entityId = els.previewEntityId && els.previewEntityId.value.trim();
  const sampleScenario = els.sampleScenario ? els.sampleScenario.value : "";
  state.sampleScenario = sampleScenario;
  const payload = {
    templateId: item.id,
    variant: item.variant || undefined,
    package: currentBody(),
  };
  if (entityId) {
    payload.entityId = entityId;
  } else if (sampleScenario) {
    payload.sampleScenario = sampleScenario;
  }
  const res = await fetch("/api/hprp/preview", {
    method: "POST",
    headers: headers(),
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const text = await res.text();
    let msg = text;
    try {
      const body = JSON.parse(text);
      msg = (body && body.errors && body.errors.join("\n")) || body.detail || body.title || text;
    } catch { /* keep text */ }
    throw new Error(msg || res.statusText);
  }
  const blob = await res.blob();
  if (state.previewUrl)
    URL.revokeObjectURL(state.previewUrl);
  state.previewUrl = URL.createObjectURL(blob);
  els.preview.classList.add("has-pdf");
  els.preview.src = state.previewUrl;
  setStatus(
    entityId
      ? `Preview from entity ${entityId} (same fetch path as print).`
      : `Preview from sample${sampleScenario ? "." + sampleScenario : ""} (print renderer + draft package).`,
    "ok"
  );
}

document.querySelectorAll(".tab").forEach((btn) => {
  btn.addEventListener("click", () => {
    if (!state.selected) return;
    try { flushJsonEditor(); } catch (err) {
      setStatus("Fix JSON before switching tabs: " + err.message, "err");
      return;
    }
    document.querySelectorAll(".tab").forEach((t) => t.classList.toggle("active", t === btn));
    state.tab = btn.dataset.tab;
    showJsonTab();
  });
});

function onClick(id, handler) {
  const el = document.getElementById(id);
  if (!el) return;
  el.addEventListener("click", handler);
}

onClick("btnModeDesigner", () => setMode("designer"));
onClick("btnModeJson", () => setMode("json"));
onClick("btnReload", () => loadList().catch((err) => setStatus(err.message, "err")));
onClick("btnPackAll", () => packAll().catch((err) => setStatus(err.message, "err")));
["btnValidate", "btnValidateJson"].forEach((id) => {
  onClick(id, () => validate().catch((err) => setStatus(err.message, "err")));
});
["btnPackThis", "btnPackThisJson"].forEach((id) => {
  onClick(id, () => packSelectedFromDisk().catch((err) => setStatus(err.message, "err")));
});
["btnSave", "btnSaveJson"].forEach((id) => {
  onClick(id, () => save().catch((err) => setStatus(err.message, "err")));
});
if (els.btnPreview)
  els.btnPreview.addEventListener("click", () => preview().catch((err) => setStatus(err.message, "err")));
if (els.sampleScenario)
  els.sampleScenario.addEventListener("change", () => schedulePreview());
if (els.previewEntityId) {
  els.previewEntityId.addEventListener("change", () => schedulePreview());
  els.previewEntityId.addEventListener("keydown", (ev) => {
    if (ev.key === "Enter") schedulePreview();
  });
}

loadList().catch((err) => {
  els.list.innerHTML = `<li class="muted">Failed to load packages</li>`;
  setStatus(err.message, "err");
});
loadCatalog().catch((err) => setStatus(err.message, "err"));
