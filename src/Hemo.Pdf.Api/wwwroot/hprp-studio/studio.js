const state = {
  list: [],
  selected: null,
  tab: "manifest",
  mode: "designer",
  draft: { manifest: {}, layout: { body: [] }, labels: {} },
  catalog: { widgets: [], blockTypes: [], sampleTemplateIds: [] },
  selectedKey: null,
  previewUrl: null,
  previewBlob: null,
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
  pageCanvas: document.getElementById("pageCanvas"),
  preview: document.getElementById("previewFrame"),
  btnPreview: document.getElementById("btnPreview"),
  btnDownloadPdf: document.getElementById("btnDownloadPdf"),
  btnExport: document.getElementById("btnExport"),
  btnImport: document.getElementById("btnImport"),
  btnLabels: document.getElementById("btnLabels"),
  fileImport: document.getElementById("fileImportHprp"),
  sampleScenario: document.getElementById("sampleScenario"),
  previewEntityId: document.getElementById("previewEntityId"),
};

function humanizeId(id) {
  if (!id) return "(empty)";
  const raw = String(id);
  const leaf = raw.includes(".") ? raw.split(".").pop() : raw;
  return leaf.replace(/[-_]/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
}

function recipeTitle(recipe) {
  if (!recipe) return "";
  return recipe.title || recipe.displayName || humanizeId(recipe.id);
}

function nodeKind(node) {
  if (!node) return "block";
  if (node.widget) return "dense";
  return "block";
}

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
  if (!key || key === "page")
    return pageObject();
  if (key === "header") return state.draft.layout.header || null;
  const hit = locate(key);
  return hit ? hit.value : null;
}

function pageObject() {
  if (!state.draft.layout.page || typeof state.draft.layout.page !== "object")
    state.draft.layout.page = { size: "A4" };
  return state.draft.layout.page;
}

function parsePath(key) {
  if (!key || key === "page") return [{ kind: "page" }];
  if (key === "header") return [{ kind: "header" }];
  return String(key).split("/").map((seg) => {
    const colon = seg.indexOf(":");
    if (colon < 0) return { kind: seg, index: 0 };
    return { kind: seg.slice(0, colon), index: Number(seg.slice(colon + 1)) };
  });
}

function locate(key) {
  ensureLayout();
  if (!key || key === "page") return { parent: state.draft.layout, prop: "page", value: pageObject() };
  if (key === "header") return { parent: state.draft.layout, prop: "header", value: state.draft.layout.header || null };
  const parts = parsePath(key);
  let parent = state.draft.layout;
  let value = parent;
  let prop = null;
  let index = null;
  for (const part of parts) {
    if (part.kind === "body" || part.kind === "sections" || part.kind === "nodes" || part.kind === "cells") {
      const list = part.kind === "body" ? parent.body
        : part.kind === "sections" ? parent.sections
        : part.kind === "cells" ? (parent.cells || [])
        : (parent.nodes || []);
      parent = list;
      index = part.index;
      prop = part.kind;
      value = list[part.index];
    } else {
      return null;
    }
  }
  return { parent, prop, index, value };
}

function setNodeAt(key, node) {
  ensureLayout();
  if (key === "page") {
    state.draft.layout.page = node;
    return;
  }
  if (key === "header") {
    state.draft.layout.header = node;
    return;
  }
  const hit = locate(key);
  if (!hit || !Array.isArray(hit.parent) || hit.index == null) return;
  if (node == null)
    hit.parent.splice(hit.index, 1);
  else
    hit.parent[hit.index] = node;
}

function parentKey(key) {
  if (!key || key === "page" || key === "header") return null;
  const i = key.lastIndexOf("/");
  if (i < 0) return "page";
  return key.slice(0, i);
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
  [
    "btnValidate", "btnSave", "btnPackThis", "btnValidateJson", "btnSaveJson", "btnPackThisJson",
    "btnPreview", "btnDownloadPdf", "btnExport", "btnLabels",
  ].forEach((id) => {
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
  const addRecipeButton = (recipe, onClick) => {
    const btn = document.createElement("button");
    btn.type = "button";
    const title = recipeTitle(recipe);
    btn.innerHTML = `<strong>${title}</strong><span class="pid">${recipe.id}</span>`;
    btn.title = (recipe.kind || "") + (recipe.allowedOn && recipe.allowedOn.length ? " · " + recipe.allowedOn.join(", ") : "");
    btn.addEventListener("click", onClick);
    els.palette.appendChild(btn);
  };
  if (slot === "sections")
    addGroup("Hemosheet sections");
  else
    addGroup("Clinical widgets");
  for (const recipe of widgets)
    addRecipeButton(recipe, () => addNode(newWidgetNode(recipe)));
  if (slot === "body") {
    addGroup("Body blocks");
    for (const recipe of state.catalog.blockTypes || [])
      addRecipeButton(recipe, () => addNode(newBlock(recipe.id)));
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
  if (type === "row")
    return {
      type: "row",
      gapMm: 2,
      cells: [
        { width: "*", nodes: [{ type: "text", content: "" }] },
        { width: "*", nodes: [{ type: "text", content: "" }] },
      ],
    };
  if (type === "column-stack")
    return { type: "column-stack", nodes: [{ type: "text", content: "" }] };
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
    const sel = state.selectedKey && locate(state.selectedKey);
    const target = sel && sel.value;
    if (target && Array.isArray(target.cells)) {
      target.cells.push({ width: "*", nodes: [node] });
      state.selectedKey = state.selectedKey + "/cells:" + (target.cells.length - 1);
    } else if (target && Array.isArray(target.nodes) && target.type === "column-stack") {
      target.nodes.push(node);
      state.selectedKey = state.selectedKey + "/nodes:" + (target.nodes.length - 1);
    } else if (sel && sel.prop === "cells" && sel.value && Array.isArray(sel.value.nodes)) {
      sel.value.nodes.push(node);
      state.selectedKey = state.selectedKey + "/nodes:" + (sel.value.nodes.length - 1);
    } else {
      state.draft.layout.body.push(node);
      state.selectedKey = "body:" + (state.draft.layout.body.length - 1);
    }
  }
  renderDesigner();
  schedulePreview();
}

function moveInParent(key, delta) {
  const hit = locate(key);
  if (!hit || !Array.isArray(hit.parent) || hit.index == null) return;
  const next = hit.index + delta;
  if (next < 0 || next >= hit.parent.length) return;
  const [item] = hit.parent.splice(hit.index, 1);
  hit.parent.splice(next, 0, item);
  const parent = parentKey(key);
  const last = parsePath(key).pop();
  state.selectedKey = (!parent || parent === "page")
    ? last.kind + ":" + next
    : parent + "/" + last.kind + ":" + next;
  renderDesigner();
  schedulePreview();
}

function moveListItem(index, delta) {
  const slot = listSlot();
  moveInParent(slot + ":" + index, delta);
}

function removeNode(key) {
  if (key === "header") {
    state.draft.layout.header = undefined;
    state.selectedKey = "page";
  } else if (key === "page") {
    return;
  } else {
    const parent = parentKey(key);
    setNodeAt(key, null);
    state.selectedKey = parent && parent !== "page" ? parent : "page";
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
      : "Structure";
  }

  const pageLi = document.createElement("li");
  pageLi.className = (!state.selectedKey || state.selectedKey === "page") ? "active" : "";
  pageLi.innerHTML = `<span class="id">Page</span><span class="meta">size / margin / font</span>`;
  pageLi.addEventListener("click", () => { state.selectedKey = "page"; renderDesigner(); });
  els.bodyList.appendChild(pageLi);

  const labelsLi = document.createElement("li");
  labelsLi.className = state.selectedKey === "labels" ? "active" : "";
  labelsLi.innerHTML = `<span class="id">Labels</span><span class="meta">labels.${labelLang()}</span>`;
  labelsLi.addEventListener("click", () => { state.selectedKey = "labels"; renderDesigner(); });
  els.bodyList.appendChild(labelsLi);

  if (slot === "body") {
    const header = state.draft.layout.header;
    if (header)
      els.bodyList.appendChild(bodyItem("header", header, { header: true }));
  }
  const list = nodeList();
  list.forEach((node, i) => appendTree(els.bodyList, slot + ":" + i, node, {
    index: i,
    count: list.length,
    slot,
  }));
  renderPageCanvas();
}

function appendTree(ul, key, node, opts) {
  ul.appendChild(bodyItem(key, node, opts));
  if (!node) return;
  if (Array.isArray(node.cells)) {
    node.cells.forEach((cell, i) => {
      const cellKey = key + "/cells:" + i;
      ul.appendChild(bodyItem(cellKey, cell, {
        index: i,
        count: node.cells.length,
        slot: "cells",
        cell: true,
        width: cell.width,
      }));
      (cell.nodes || []).forEach((child, n) => {
        appendTree(ul, cellKey + "/nodes:" + n, child, {
          index: n,
          count: (cell.nodes || []).length,
          slot: "nodes",
          nested: true,
        });
      });
    });
  } else if (node.type === "column-stack" && Array.isArray(node.nodes)) {
    node.nodes.forEach((child, n) => {
      appendTree(ul, key + "/nodes:" + n, child, {
        index: n,
        count: node.nodes.length,
        slot: "nodes",
        nested: true,
      });
    });
  }
}

function bodyItem(key, node, opts) {
  const li = document.createElement("li");
  li.className = key === state.selectedKey ? "active" : "";
  if (opts.header) li.classList.add("header-node");
  if (opts.nested || opts.cell) li.classList.add("nested");
  if (opts.cell) li.classList.add("cell-node");
  if (isDenseHemosheetForm() && node && node.widget === DIALYSIS_WIDGET)
    li.classList.add("preview-affects");
  const title = opts.cell
    ? "cell " + (opts.width || "*")
    : nodeTitle(node);
  const slotLabel = opts.header ? "header" : ((opts.slot || "body") + "[" + opts.index + "]");
  li.innerHTML = `<span class="id">${title}</span><span class="meta">${slotLabel}</span>`;
  if (!opts.header) {
    li.draggable = true;
    li.addEventListener("dragstart", () => { state.dragKey = key; });
    li.addEventListener("dragover", (e) => e.preventDefault());
    li.addEventListener("drop", (e) => {
      e.preventDefault();
      if (!state.dragKey || state.dragKey === key) return;
      dropOnto(state.dragKey, key, opts);
      state.dragKey = null;
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
    up.addEventListener("click", (e) => { e.stopPropagation(); moveInParent(key, -1); });
    const down = document.createElement("button");
    down.type = "button";
    down.textContent = "Down";
    down.disabled = opts.index === opts.count - 1;
    down.addEventListener("click", (e) => { e.stopPropagation(); moveInParent(key, 1); });
    actions.append(up, down);
    if (!usesSectionsMode() && !opts.cell) {
      const beside = document.createElement("button");
      beside.type = "button";
      beside.textContent = "Place beside";
      beside.addEventListener("click", (e) => { e.stopPropagation(); placeBeside(key); });
      actions.append(beside);
      if (String(key).includes("/cells:")) {
        const br = document.createElement("button");
        br.type = "button";
        br.textContent = "Break row";
        br.addEventListener("click", (e) => { e.stopPropagation(); breakRow(key); });
        actions.append(br);
      }
    }
  }
  const del = document.createElement("button");
  del.type = "button";
  del.textContent = "Remove";
  del.addEventListener("click", (e) => { e.stopPropagation(); removeNode(key); });
  actions.append(del);
  li.appendChild(actions);
  return li;
}

function dropOnto(fromKey, toKey) {
  const from = locate(fromKey);
  if (!from || from.value == null || !Array.isArray(from.parent)) return;
  const moving = from.value;
  from.parent.splice(from.index, 1);
  const to = locate(toKey);
  if (to && to.value && Array.isArray(to.value.cells)) {
    to.value.cells.push({ width: "*", nodes: [moving] });
  } else if (to && Array.isArray(to.parent)) {
    to.parent.splice(Math.min(to.index + 1, to.parent.length), 0, moving);
  } else {
    nodeList().push(moving);
  }
  renderDesigner();
  schedulePreview();
}

function placeBeside(key) {
  if (usesSectionsMode()) return;
  const hit = locate(key);
  if (!hit || !hit.value || hit.prop === "cells") return;
  const node = hit.value;
  if (!Array.isArray(hit.parent)) return;
  const row = {
    type: "row",
    gapMm: 2,
    cells: [
      { width: "*", nodes: [node] },
      { width: "*", nodes: [{ type: "text", content: "" }] },
    ],
  };
  hit.parent[hit.index] = row;
  state.selectedKey = key + "/cells:1/nodes:0";
  renderDesigner();
  schedulePreview();
}

function breakRow(key) {
  const parts = parsePath(key);
  const cellPart = parts.findIndex((p) => p.kind === "cells");
  if (cellPart < 0) return;
  const rowKey = parts.slice(0, cellPart).map((p) => p.kind + ":" + p.index).join("/");
  const nodeKey = key.includes("/nodes:") ? key : null;
  const rowHit = locate(rowKey);
  const nodeHit = nodeKey ? locate(nodeKey) : locate(key);
  if (!rowHit || !rowHit.value || !Array.isArray(rowHit.parent)) return;
  const lifted = nodeHit && nodeHit.value && nodeHit.prop === "nodes"
    ? nodeHit.value
    : (nodeHit && nodeHit.value && nodeHit.value.nodes && nodeHit.value.nodes[0]);
  if (!lifted) return;
  if (nodeHit && Array.isArray(nodeHit.parent) && nodeHit.prop === "nodes")
    nodeHit.parent.splice(nodeHit.index, 1);
  rowHit.parent.splice(rowHit.index + 1, 0, lifted);
  const parent = parentKey(rowKey);
  const last = parsePath(rowKey).pop();
  state.selectedKey = (!parent || parent === "page")
    ? last.kind + ":" + (rowHit.index + 1)
    : parent + "/" + last.kind + ":" + (rowHit.index + 1);
  renderDesigner();
  schedulePreview();
}

function renderSchematic() {
  /* Schematic pane replaced by Page canvas; kept as no-op for callers. */
}

function pageOrientation() {
  const page = pageObject();
  return String(page.orientation || "").toLowerCase() === "landscape" ? "landscape" : "portrait";
}

function selectCanvasKey(key) {
  state.selectedKey = key;
  renderDesigner();
}

function pageCardMeta(node, key) {
  const bits = [key];
  if (node && node.when) bits.push("when: " + node.when);
  if (node && node.widget) bits.push("dense C#");
  return bits.join(" · ");
}

function buildPageCard(key, node) {
  if (node && node.type === "row" && Array.isArray(node.cells)) {
    const wrap = document.createElement("div");
    wrap.className = "page-card-row";
    node.cells.forEach((cell, i) => {
      const cellKey = key + "/cells:" + i;
      const cellEl = document.createElement("div");
      cellEl.className = "page-card-cell" + (state.selectedKey === cellKey ? " active" : "");
      cellEl.style.flex = cell.width && String(cell.width).endsWith("%")
        ? String(cell.width).slice(0, -1)
        : "1";
      cellEl.addEventListener("click", (e) => {
        e.stopPropagation();
        selectCanvasKey(cellKey);
      });
      (cell.nodes || []).forEach((child, n) => {
        cellEl.appendChild(buildPageCard(cellKey + "/nodes:" + n, child));
      });
      wrap.appendChild(cellEl);
    });
    return wrap;
  }

  if (node && node.type === "column-stack" && Array.isArray(node.nodes)) {
    const stack = document.createElement("div");
    stack.className = "page-card block" + (key === state.selectedKey ? " active" : "");
    stack.innerHTML = `<div>${nodeTitle(node)}</div><div class="meta">${pageCardMeta(node, key)}</div>`;
    stack.addEventListener("click", (e) => {
      e.stopPropagation();
      selectCanvasKey(key);
    });
    node.nodes.forEach((child, n) => {
      stack.appendChild(buildPageCard(key + "/nodes:" + n, child));
    });
    return stack;
  }

  const el = document.createElement("div");
  const kind = nodeKind(node);
  el.className = "page-card " + kind + (key === state.selectedKey ? " active" : "");
  el.innerHTML = `<div>${nodeTitle(node)}</div><div class="meta">${pageCardMeta(node, key)}</div>`;
  el.draggable = true;
  el.addEventListener("click", (e) => {
    e.stopPropagation();
    selectCanvasKey(key);
  });
  el.addEventListener("dragstart", (e) => {
    e.dataTransfer.setData("text/plain", key);
    e.dataTransfer.effectAllowed = "move";
  });
  el.addEventListener("dragover", (e) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
  });
  el.addEventListener("drop", (e) => {
    e.preventDefault();
    e.stopPropagation();
    const from = e.dataTransfer.getData("text/plain");
    if (from) dropOnto(from, key);
  });
  return el;
}

function renderPageCanvas() {
  if (!els.pageCanvas) return;
  els.pageCanvas.innerHTML = "";
  if (!state.selected) {
    els.pageCanvas.innerHTML = `<p class="muted">Select a package</p>`;
    return;
  }
  ensureLayout();
  const sheet = document.createElement("div");
  sheet.className = "page-sheet " + pageOrientation();
  const label = document.createElement("div");
  label.className = "page-sheet-label";
  label.textContent = "A4 " + pageOrientation() + " · " + (usesSectionsMode() ? "sections" : "body");
  sheet.appendChild(label);

  if (!usesSectionsMode() && state.draft.layout.header) {
    sheet.appendChild(buildPageCard("header", state.draft.layout.header));
  }

  const nodes = usesSectionsMode()
    ? (state.draft.layout.sections || [])
    : (state.draft.layout.body || []);
  const prefix = usesSectionsMode() ? "sections:" : "body:";
  if (!nodes.length) {
    const empty = document.createElement("p");
    empty.className = "muted";
    empty.textContent = usesSectionsMode()
      ? "No sections — add from palette (stacked order only; Place beside is off)."
      : "No body nodes — add from palette.";
    sheet.appendChild(empty);
  } else {
    nodes.forEach((node, i) => sheet.appendChild(buildPageCard(prefix + i, node)));
  }

  els.pageCanvas.appendChild(sheet);
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
  if (!state.selectedKey || state.selectedKey === "page") {
    const page = pageObject();
    mutator(page);
    state.draft.layout.page = page;
    renderDesigner();
    schedulePreview();
    return;
  }
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

function renderPageInspector() {
  const page = pageObject();
  const head = document.createElement("p");
  head.innerHTML = "<strong>Page</strong><br/><span class=\"muted\">มีผลเมื่อไฟล์ระบุค่า — ว่าง = ค่า C# เดิม</span>";
  els.inspector.appendChild(head);
  els.inspector.appendChild(field("size", textInput(page.size || "A4", (v) => mutateSelected((p) => { p.size = v.trim() || "A4"; }))));
  const margin = page.margin || {};
  const setSide = (side, v) => mutateSelected((p) => {
    p.margin = { ...(p.margin || {}) };
    if (v == null) delete p.margin[side];
    else p.margin[side] = v;
    if (!p.margin.top && p.margin.top !== 0 && !p.margin.right && p.margin.right !== 0
      && !p.margin.bottom && p.margin.bottom !== 0 && !p.margin.left && p.margin.left !== 0)
      delete p.margin;
  });
  els.inspector.appendChild(field("margin.top mm", numberInput(margin.top, (v) => setSide("top", v))));
  els.inspector.appendChild(field("margin.right mm", numberInput(margin.right, (v) => setSide("right", v))));
  els.inspector.appendChild(field("margin.bottom mm", numberInput(margin.bottom, (v) => setSide("bottom", v))));
  els.inspector.appendChild(field("margin.left mm", numberInput(margin.left, (v) => setSide("left", v))));
  els.inspector.appendChild(field("marginMm (shorthand ทุกด้าน)", numberInput(page.marginMm, (v) => mutateSelected((p) => {
    if (v == null) delete p.marginMm;
    else p.marginMm = v;
  }))));
  els.inspector.appendChild(field("spacingMm", numberInput(page.spacingMm, (v) => mutateSelected((p) => {
    if (v == null) delete p.spacingMm;
    else p.spacingMm = v;
  }))));
  const spacingHint = document.createElement("p");
  spacingHint.className = "muted";
  spacingHint.textContent = "clinical-05: ระยะใต้ thaiur.header ก่อนตาราง — ใส่ 0 เพื่อให้กรอบชิดกัน";
  els.inspector.appendChild(spacingHint);
  els.inspector.appendChild(field("fontSize", numberInput(page.fontSize, (v) => mutateSelected((p) => {
    if (v == null) delete p.fontSize;
    else p.fontSize = v;
  }))));
}

function renderCellInspector(cell) {
  const head = document.createElement("p");
  head.innerHTML = "<strong>Row cell</strong><br/><span class=\"muted\">width: * / 40% / 32mm</span>";
  els.inspector.appendChild(head);
  els.inspector.appendChild(field("width", textInput(cell.width || "*", (v) => mutateSelected((n) => {
    n.width = v.trim() || "*";
  }))));
}

function renderCapabilityStrip(node, recipe) {
  const note = document.createElement("p");
  note.className = "inspector-note";
  if (node.type === "row") {
    note.textContent = "แถว: ลูกใน cells อยู่แถวเดียวกัน — Place beside สร้างแถวนี้ / Break row ดึงบล็อกออกมา";
  } else if (recipe && (recipe.inspectorFields || []).includes("chrome.headerAlign")) {
    note.textContent = "แถบหัวคอลัมน์: headerAlign=top ชิดขอบบน · headerHeightMm / headerPaddingMm ปรับความสูงและ inset";
  } else if (recipe && recipe.kind === "dense" && !(recipe.inspectorFields || []).includes("columnPlan")
      && !(recipe.inspectorFields || []).includes("columns")) {
    note.textContent = "Widget หนาแน่น: แก้ได้เฉพาะ knobs ใน inspector (chrome/when) — พิกเซลในตารางยังเป็น C#";
  } else if (recipe && (recipe.inspectorFields || []).includes("columnPlan")) {
    note.textContent = "คอลัมน์ตารางข้อมูล (columnPlan) — เพิ่ม/ลบได้เฉพาะ bind ที่สูตรยอม";
  } else if (node.type === "field-grid") {
    note.textContent = "คอลัมน์กริด label/value — จำนวนช่อง + columnSpan ต่อฟิลด์ ไม่ใช่คอลัมน์ตารางข้อมูล";
  } else {
    return;
  }
  els.inspector.appendChild(note);
}

function renderBoxInspector(node) {
  const box = node.box || {};
  const title = document.createElement("p");
  title.innerHTML = "<strong>box</strong>";
  els.inspector.appendChild(title);
  const marginVal = box.marginMm == null ? "" : (Array.isArray(box.marginMm) ? box.marginMm.join(",") : String(box.marginMm));
  els.inspector.appendChild(field("box.marginMm", textInput(marginVal, (v) => mutateSelected((n) => {
    n.box = { ...(n.box || {}) };
    const parsed = parseInset(v);
    if (parsed == null) delete n.box.marginMm;
    else n.box.marginMm = parsed;
    if (!n.box.marginMm && n.box.paddingMm == null) delete n.box;
  }))));
  const padVal = box.paddingMm == null ? "" : (Array.isArray(box.paddingMm) ? box.paddingMm.join(",") : String(box.paddingMm));
  els.inspector.appendChild(field("box.paddingMm", textInput(padVal, (v) => mutateSelected((n) => {
    n.box = { ...(n.box || {}) };
    const parsed = parseInset(v);
    if (parsed == null) delete n.box.paddingMm;
    else n.box.paddingMm = parsed;
    if (!n.box.paddingMm && n.box.marginMm == null) delete n.box;
  }))));
}

function parseInset(raw) {
  const v = String(raw || "").trim();
  if (!v) return null;
  if (v.includes(",")) {
    const parts = v.split(",").map((s) => Number(s.trim())).filter((n) => Number.isFinite(n));
    return parts.length ? parts : null;
  }
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

function renderFieldGridFields(node) {
  const title = document.createElement("p");
  title.innerHTML = "<strong>fields</strong>";
  els.inspector.appendChild(title);
  const fields = Array.isArray(node.fields) ? node.fields : [];
  fields.forEach((item, i) => {
    const card = document.createElement("div");
    card.className = "col-card";
    const labelVal = item && item.label && typeof item.label === "object" && item.label.$label
      ? "$" + item.label.$label
      : (item && typeof item.label === "string" ? item.label : "");
    card.appendChild(field("label", textInput(labelVal, (v) => mutateSelected((n) => {
      const copy = [...(n.fields || [])];
      const next = { ...(copy[i] || {}) };
      if (v.startsWith("$") && v.length > 1) next.label = { $label: v.slice(1) };
      else next.label = v;
      copy[i] = next;
      n.fields = copy;
    }))));
    card.appendChild(field("bind", textInput((item && item.bind) || "", (v) => mutateSelected((n) => {
      const copy = [...(n.fields || [])];
      copy[i] = { ...(copy[i] || {}), bind: v };
      n.fields = copy;
    }))));
    card.appendChild(field("columnSpan", numberInput((item && item.columnSpan) || 1, (v) => mutateSelected((n) => {
      const copy = [...(n.fields || [])];
      copy[i] = { ...(copy[i] || {}), columnSpan: v && v > 0 ? Math.round(v) : 1 };
      n.fields = copy;
    }))));
    const del = document.createElement("button");
    del.type = "button";
    del.textContent = "Remove field";
    del.addEventListener("click", () => mutateSelected((n) => {
      n.fields = (n.fields || []).filter((_, idx) => idx !== i);
    }));
    card.appendChild(del);
    els.inspector.appendChild(card);
  });
  const add = document.createElement("button");
  add.type = "button";
  add.textContent = "Add field";
  add.addEventListener("click", () => mutateSelected((n) => {
    n.fields = [...(n.fields || []), { label: "", bind: "", columnSpan: 1 }];
  }));
  els.inspector.appendChild(add);
}

function renderLabelsInspector() {
  ensureLayout();
  const lang = labelLang();
  const map = labelMap();
  const wrap = document.createElement("div");
  wrap.className = "labels-editor";
  const head = document.createElement("p");
  head.innerHTML = `<strong>Labels · ${lang}</strong>`;
  wrap.appendChild(head);
  const hint = document.createElement("p");
  hint.className = "muted";
  hint.textContent = "แก้ข้อความที่ใช้ผ่าน $label โดยไม่ต้องเปิด JSON mode";
  wrap.appendChild(hint);

  const keys = Object.keys(map).sort((a, b) => a.localeCompare(b));
  if (!keys.length) {
    const empty = document.createElement("p");
    empty.className = "muted";
    empty.textContent = "ยังไม่มีคีย์ — เพิ่มด้านล่าง หรือเลือก dense widget ที่มี labelKeys";
    wrap.appendChild(empty);
  }
  for (const key of keys) {
    const row = document.createElement("div");
    row.className = "label-row";
    const keyEl = document.createElement("code");
    keyEl.textContent = key;
    const input = textInput(map[key] || "", (v) => {
      if (!state.draft.labels[lang]) state.draft.labels[lang] = {};
      if (v.trim()) state.draft.labels[lang][key] = v;
      else delete state.draft.labels[lang][key];
      schedulePreview();
    });
    row.append(keyEl, input);
    wrap.appendChild(row);
  }

  const addRow = document.createElement("div");
  addRow.className = "label-row";
  const newKey = document.createElement("input");
  newKey.type = "text";
  newKey.placeholder = "newKey";
  const newVal = document.createElement("input");
  newVal.type = "text";
  newVal.placeholder = "value";
  const addBtn = document.createElement("button");
  addBtn.type = "button";
  addBtn.textContent = "Add";
  addBtn.addEventListener("click", () => {
    const k = newKey.value.trim();
    if (!k) return;
    if (!state.draft.labels[lang]) state.draft.labels[lang] = {};
    state.draft.labels[lang][k] = newVal.value;
    renderDesigner();
    schedulePreview();
  });
  addRow.append(newKey, newVal);
  wrap.appendChild(addRow);
  wrap.appendChild(addBtn);
  els.inspector.appendChild(wrap);
}

function renderInspector() {
  const key = state.selectedKey;
  els.inspector.innerHTML = "";
  if (key === "labels") {
    renderLabelsInspector();
    return;
  }
  if (!key || key === "page") {
    renderPageInspector();
    return;
  }
  const node = nodeAt(key);
  if (!node) {
    els.inspector.innerHTML = `<p class="muted">เลือกบล็อกในรายการ Page canvas หรือกด Page / Labels</p>`;
    return;
  }
  if (String(key).includes("/cells:") && Array.isArray(node.nodes) && !node.type && !node.widget) {
    renderCellInspector(node);
    return;
  }
  const recipe = recipeFor(node);
  const head = document.createElement("p");
  head.innerHTML = `<strong>${nodeTitle(node)}</strong><br/><span class="muted">${recipe ? recipe.kind : "custom"} · ${humanizeId(node.widget || node.type || "")}</span>`;
  els.inspector.appendChild(head);

  if (node.widget) {
    const dense = document.createElement("p");
    dense.className = "inspector-dense-note";
    dense.textContent = "กล่องนี้วาดใน C# (dense widget) — ปรับ chrome / labels / ลำดับเท่านั้น ไม่วาดพิกเซลภายในที่นี่";
    els.inspector.appendChild(dense);
  }

  renderCapabilityStrip(node, recipe);

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
        && c.headerHeightMm == null
        && !c.headerAlign
        && c.headerPaddingMm == null
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

    if (fields.includes("chrome.headerHeightMm")) {
      const h = numberInput(chrome.headerHeightMm, (v) => mutateSelected((n) => {
        n.chrome = { ...(n.chrome || {}), headerHeightMm: v };
        clearChromeIfEmpty(n);
      }));
      h.placeholder = "ว่าง = ความสูงแถบหัวเดิม (mm)";
      els.inspector.appendChild(field("chrome.headerHeightMm", h));
    }

    if (fields.includes("chrome.headerAlign")) {
      els.inspector.appendChild(field("chrome.headerAlign", selectInput(chrome.headerAlign || "", [
        { value: "", label: "(default middle)" },
        { value: "top", label: "top — ชิดขอบบน" },
        { value: "middle", label: "middle" },
        { value: "bottom", label: "bottom" },
      ], (v) => mutateSelected((n) => {
        n.chrome = { ...(n.chrome || {}), headerAlign: v || undefined };
        clearChromeIfEmpty(n);
      }))));
    }

    if (fields.includes("chrome.headerPaddingMm")) {
      const pad = numberInput(chrome.headerPaddingMm, (v) => mutateSelected((n) => {
        n.chrome = { ...(n.chrome || {}), headerPaddingMm: v };
        clearChromeIfEmpty(n);
      }));
      pad.placeholder = "inset ในเซลล์หัว (mm)";
      els.inspector.appendChild(field("chrome.headerPaddingMm", pad));
    }

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

  if (fields.includes("columns") && node.type === "field-grid") {
    els.inspector.appendChild(field("field-grid columns", numberInput(node.columns || 2, (v) => mutateSelected((n) => {
      n.columns = v && v > 0 ? Math.round(v) : 2;
    }))));
  }

  if (fields.includes("columns") && !fields.includes("columnPlan") && node.type !== "field-grid"
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

  if (fields.includes("bind")) {
    els.inspector.appendChild(field("bind", textInput(node.bind || "", (v) => mutateSelected((n) => {
      if (v.trim()) n.bind = v.trim();
      else delete n.bind;
    }))));
  }

  if (fields.includes("style")) {
    els.inspector.appendChild(field("style", selectInput(node.style || "body", [
      { value: "body", label: "body" },
      { value: "title", label: "title" },
      { value: "subtitle", label: "subtitle" },
    ], (v) => mutateSelected((n) => { n.style = v; }))));
  }

  if (fields.includes("bindRows")) {
    els.inspector.appendChild(field("bindRows", textInput(node.bindRows || "", (v) => mutateSelected((n) => {
      if (v.trim()) n.bindRows = v.trim();
      else delete n.bindRows;
    }))));
  }

  if (fields.includes("fields") && node.type === "field-grid")
    renderFieldGridFields(node);

  if (fields.includes("gapMm") || node.type === "row") {
    els.inspector.appendChild(field("gapMm", numberInput(node.gapMm, (v) => mutateSelected((n) => {
      if (v == null) delete n.gapMm;
      else n.gapMm = v;
    }))));
  }

  if (!usesSectionsMode())
    renderBoxInspector(node);

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
  if (window.AbsoluteDesigner && AbsoluteDesigner.isAbsoluteMode()) {
    AbsoluteDesigner.syncBodyClass();
    AbsoluteDesigner.renderAbsoluteDesigner();
    return;
  }
  if (window.AbsoluteDesigner)
    AbsoluteDesigner.syncBodyClass();
  renderDenseFormHint();
  renderPalette();
  renderBodyList();
  renderInspector();
}

function triggerDownload(blob, filename) {
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 2000);
}

async function fetchPreviewBlob() {
  const item = state.selected;
  if (!item) throw new Error("Select a package first.");
  const entityId = els.previewEntityId && els.previewEntityId.value.trim();
  const sampleScenario = els.sampleScenario ? els.sampleScenario.value : "";
  state.sampleScenario = sampleScenario;
  const payload = {
    templateId: item.id,
    variant: item.variant || undefined,
    package: currentBody(),
  };
  if (entityId)
    payload.entityId = entityId;
  else if (sampleScenario)
    payload.sampleScenario = sampleScenario;

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
  return res.blob();
}

async function preview() {
  const item = state.selected;
  if (!item) return null;
  const entityId = els.previewEntityId && els.previewEntityId.value.trim();
  const sampleScenario = els.sampleScenario ? els.sampleScenario.value : "";
  const blob = await fetchPreviewBlob();
  state.previewBlob = blob;
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
  return blob;
}

async function downloadPdf() {
  const item = state.selected;
  if (!item) return;
  const blob = await preview();
  if (!blob) return;
  const name = `${item.id}${item.variant ? "." + item.variant : ""}.pdf`;
  triggerDownload(blob, name);
  setStatus("Downloaded " + name + " (same bytes as preview).", "ok");
}

async function exportHprp() {
  if (typeof JSZip === "undefined")
    throw new Error("JSZip failed to load.");
  const body = currentBody();
  await api("/api/hprp/validate", { method: "POST", body: JSON.stringify(body) });
  const zip = new JSZip();
  zip.file("manifest.json", JSON.stringify(body.manifest || {}, null, 2));
  zip.file("layout.json", JSON.stringify(body.layout || {}, null, 2));
  const labels = body.labels || {};
  for (const [lang, map] of Object.entries(labels)) {
    if (!map || typeof map !== "object") continue;
    zip.file(`labels.${lang}.json`, JSON.stringify(map, null, 2));
  }
  const blob = await zip.generateAsync({ type: "blob", compression: "DEFLATE" });
  const id = (body.manifest && body.manifest.id) || (state.selected && state.selected.id) || "package";
  const variant = (body.manifest && body.manifest.variant) || (state.selected && state.selected.variant);
  const filename = variant && String(variant).toLowerCase() !== "default"
    ? `${id}.${variant}.hprp`
    : `${id}.hprp`;
  triggerDownload(blob, filename);
  setStatus("Exported " + filename + " (validated).", "ok");
}

async function importHprpFile(file) {
  if (typeof JSZip === "undefined")
    throw new Error("JSZip failed to load.");
  const zip = await JSZip.loadAsync(file);
  const manifestStr = await zip.file("manifest.json")?.async("string");
  const layoutStr = await zip.file("layout.json")?.async("string");
  if (!manifestStr || !layoutStr)
    throw new Error("Invalid .hprp: need manifest.json and layout.json");
  const manifest = JSON.parse(manifestStr);
  const layout = JSON.parse(layoutStr);
  const labels = {};
  for (const [path, entry] of Object.entries(zip.files)) {
    if (entry.dir) continue;
    const name = path.split("/").pop();
    const m = /^labels\.([^.]+)\.json$/i.exec(name);
    if (!m) continue;
    labels[m[1]] = JSON.parse(await entry.async("string"));
  }
  const body = { manifest, layout, labels };
  try {
    await api("/api/hprp/validate", { method: "POST", body: JSON.stringify(body) });
  } catch (err) {
    throw new Error("Import failed validation:\n" + err.message);
  }
  state.draft = {
    manifest: manifest || {},
    layout: layout || { body: [] },
    labels: labels || {},
  };
  ensureLayout();
  const id = manifest.id;
  const variant = manifest.variant || null;
  let item = (state.list || []).find((p) =>
    p.id === id && (p.variant || null) === (variant || null));
  if (!item) {
    item = {
      id,
      variant,
      displayName: manifest.displayName || id,
      layoutKind: manifest.layoutKind,
      layoutProfile: manifest.layoutProfile,
      profileLabel: manifest.ui && manifest.ui.profileLabel,
      packed: false,
      sourcePath: file.name,
    };
  }
  state.selected = item;
  state.selectedKey = "page";
  selectPackage(item);
  const label = profileLabel(item);
  const kind = item.layoutKind || state.draft.manifest.layoutKind || "";
  els.title.textContent = `${item.id} · ${label}${kind ? " (" + kind + ")" : ""} (imported)`;
  setButtonsEnabled(true);
  if (state.mode === "json") showJsonTab();
  else renderDesigner();
  await loadSampleScenarios(item.id);
  schedulePreview();
  setStatus("Imported " + file.name + " (validated). Use Save and pack to write packages/.", "ok");
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
  state.selectedKey = "page";
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

onClick("btnPage", () => { state.selectedKey = "page"; renderDesigner(); });
onClick("btnLabels", () => { state.selectedKey = "labels"; renderDesigner(); });
onClick("btnModeDesigner", () => setMode("designer"));
onClick("btnModeJson", () => setMode("json"));
onClick("btnReload", () => loadList().catch((err) => setStatus(err.message, "err")));
onClick("btnPackAll", () => packAll().catch((err) => setStatus(err.message, "err")));
onClick("btnExport", () => exportHprp().catch((err) => setStatus(err.message, "err")));
onClick("btnImport", () => els.fileImport && els.fileImport.click());
onClick("btnDownloadPdf", () => downloadPdf().catch((err) => setStatus(err.message, "err")));
if (els.fileImport) {
  els.fileImport.addEventListener("change", () => {
    const file = els.fileImport.files && els.fileImport.files[0];
    if (!file) return;
    importHprpFile(file)
      .catch((err) => setStatus(err.message, "err"))
      .finally(() => { els.fileImport.value = ""; });
  });
}
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
