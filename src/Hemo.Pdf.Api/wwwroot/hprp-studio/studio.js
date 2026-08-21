const state = {
  list: [],
  selected: null,
  tab: "manifest",
  draft: { manifest: {}, layout: {}, labels: {} },
};

const els = {
  token: document.getElementById("token"),
  tenant: document.getElementById("tenant"),
  list: document.getElementById("packageList"),
  catalog: document.getElementById("catalogBox"),
  editor: document.getElementById("jsonEditor"),
  title: document.getElementById("editorTitle"),
  status: document.getElementById("status"),
  validate: document.getElementById("btnValidate"),
  save: document.getElementById("btnSave"),
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

function readEditor() {
  return JSON.parse(els.editor.value);
}

function showTab() {
  els.editor.value = pretty(state.draft[state.tab]);
}

function selectPackage(item) {
  state.selected = item;
  [...els.list.children].forEach((li) => {
    li.classList.toggle("active", li.dataset.key === keyOf(item));
  });
}

function keyOf(item) {
  return `${item.id}#${item.variant || "default"}`;
}

async function loadList() {
  const items = await api("/api/hprp/packages");
  state.list = items || [];
  els.list.innerHTML = "";
  for (const item of state.list) {
    const li = document.createElement("li");
    li.dataset.key = keyOf(item);
    li.innerHTML = `<span class="id">${item.displayName || item.id}</span><span class="meta">${item.id}${item.variant ? " · " + item.variant : ""} · ${item.packed ? "packed" : "folder"}</span>`;
    li.addEventListener("click", () => openPackage(item));
    els.list.appendChild(li);
  }
}

async function loadCatalog() {
  const catalog = await api("/api/hprp/catalog");
  els.catalog.textContent = pretty(catalog);
}

async function openPackage(item) {
  const query = item.variant ? `?variant=${encodeURIComponent(item.variant)}` : "";
  const pkg = await api(`/api/hprp/packages/${encodeURIComponent(item.id)}${query}`);
  state.draft = {
    manifest: pkg.manifest || {},
    layout: pkg.layout || {},
    labels: pkg.labels || {},
  };
  selectPackage(item);
  els.title.textContent = `${item.id}${item.variant ? " (" + item.variant + ")" : ""}`;
  els.editor.disabled = false;
  els.validate.disabled = false;
  els.save.disabled = false;
  showTab();
  setStatus("Loaded " + item.id, "ok");
}

function currentBody() {
  flushEditor();
  return {
    manifest: state.draft.manifest,
    layout: state.draft.layout,
    labels: state.draft.labels,
  };
}

function flushEditor() {
  state.draft[state.tab] = readEditor();
}

async function validate() {
  const body = currentBody();
  await api("/api/hprp/validate", { method: "POST", body: JSON.stringify(body) });
  setStatus("Valid package.", "ok");
}

async function save() {
  const body = currentBody();
  const item = state.selected;
  const query = item.variant ? `?variant=${encodeURIComponent(item.variant)}` : "";
  const result = await api(`/api/hprp/packages/${encodeURIComponent(item.id)}${query}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
  setStatus("Packed " + result.outputPath, "ok");
  await loadList();
}

async function packAll() {
  const result = await api("/api/hprp/pack-from-templates", { method: "POST" });
  setStatus(`Packed ${result.length} package(s) from assets/templates/reports.`, "ok");
  await loadList();
}

document.querySelectorAll(".tab").forEach((btn) => {
  btn.addEventListener("click", () => {
    if (!state.selected) return;
    try { flushEditor(); } catch (err) {
      setStatus("Fix JSON before switching tabs: " + err.message, "err");
      return;
    }
    document.querySelectorAll(".tab").forEach((t) => t.classList.toggle("active", t === btn));
    state.tab = btn.dataset.tab;
    showTab();
  });
});

document.getElementById("btnReload").addEventListener("click", () => loadList().catch((err) => setStatus(err.message, "err")));
document.getElementById("btnPackAll").addEventListener("click", () => packAll().catch((err) => setStatus(err.message, "err")));
els.validate.addEventListener("click", () => validate().catch((err) => setStatus(err.message, "err")));
els.save.addEventListener("click", () => save().catch((err) => setStatus(err.message, "err")));

Promise.all([loadList(), loadCatalog()]).catch((err) => setStatus(err.message, "err"));
