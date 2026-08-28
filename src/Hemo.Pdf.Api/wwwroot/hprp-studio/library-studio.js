/**
 * HPRP Studio — left pane Library tab (headers / tables / fragments).
 */
(function (global) {
  let selectedKind = "headers";
  let selectedId = null;
  let filterTag = "";
  /** Set by studio.js — opens header alone on the canvas. */
  let openHeaderFn = null;

  function $(id) {
    return document.getElementById(id);
  }

  function catalog() {
    return (global.TableDesigner && global.TableDesigner.getCatalogSnapshot)
      ? global.TableDesigner.getCatalogSnapshot()
      : { headers: {}, tables: {}, fragments: {} };
  }

  function itemsForKind(kind) {
    const c = catalog();
    const map = kind === "headers" ? c.headers : kind === "tables" ? c.tables : c.fragments;
    return Object.keys(map || {}).map((id) => map[id]).filter(Boolean);
  }

  function matchesTag(item) {
    if (!filterTag) return true;
    const tags = item.tags || [];
    const q = filterTag.toLowerCase();
    return tags.some((t) => String(t).toLowerCase().indexOf(q) >= 0)
      || String(item.id || "").toLowerCase().indexOf(q) >= 0
      || String(item.displayName || "").toLowerCase().indexOf(q) >= 0;
  }

  function openHeaderOnCanvas(presetId) {
    if (typeof openHeaderFn === "function") {
      openHeaderFn(presetId);
      return;
    }
    if (typeof global.openLibraryHeader === "function") {
      global.openLibraryHeader(presetId);
      return;
    }
    console.error("[LibraryStudio] openHeader handler missing — hard-refresh Studio (Ctrl+F5)");
    alert("ยังโหลดตัวแก้ Library ไม่ครบ — กด Ctrl+F5 แล้วลองคลิก Header อีกครั้ง");
  }

  function renderList() {
    const list = $("libraryList");
    if (!list) return;
    list.innerHTML = "";
    const items = itemsForKind(selectedKind).filter(matchesTag)
      .sort((a, b) => String(a.displayName || a.id).localeCompare(String(b.displayName || b.id)));
    if (!items.length) {
      const empty = document.createElement("li");
      empty.className = "muted";
      empty.textContent = "ไม่มีรายการ";
      list.appendChild(empty);
      return;
    }
    items.forEach((item) => {
      const li = document.createElement("li");
      li.className = "lib-item" + (selectedId === item.id ? " active" : "");
      li.dataset.id = item.id;
      li.title = selectedKind === "headers"
        ? "คลิกเพื่อแก้บน canvas"
        : "คลิกเพื่อเลือก · Insert into pack เพื่อใส่ในรายงาน";
      const title = document.createElement("strong");
      title.textContent = item.displayName || item.id;
      const meta = document.createElement("span");
      meta.className = "muted";
      const bits = [item.id];
      if (item.tags && item.tags.length) bits.push(item.tags.join(", "));
      if (selectedKind === "fragments" && item.elements) bits.push(item.elements.length + " els");
      meta.textContent = bits.join(" · ");
      li.appendChild(title);
      li.appendChild(document.createElement("br"));
      li.appendChild(meta);
      li.addEventListener("click", (ev) => {
        ev.preventDefault();
        ev.stopPropagation();
        selectedId = item.id;
        // Highlight without full rebuild (rebuild raced async open).
        list.querySelectorAll(".lib-item").forEach((node) => {
          node.classList.toggle("active", node.dataset.id === item.id);
        });
        if (selectedKind === "headers") {
          openHeaderOnCanvas(item.id);
        }
      });
      list.appendChild(li);
    });
  }

  function setSideTab(side) {
    document.querySelectorAll(".side-tab").forEach((btn) => {
      btn.classList.toggle("active", btn.getAttribute("data-side") === side);
    });
    const pkg = $("packagesPanel");
    const lib = $("libraryPanel");
    if (pkg) pkg.classList.toggle("hidden", side !== "packages");
    if (lib) lib.classList.toggle("hidden", side !== "library");
    if (side === "library") renderList();
  }

  function setLibKind(kind) {
    selectedKind = kind;
    selectedId = null;
    document.querySelectorAll(".lib-subtab").forEach((btn) => {
      btn.classList.toggle("active", btn.getAttribute("data-lib") === kind);
    });
    renderList();
  }

  function insertSelected() {
    if (!selectedId) {
      alert("เลือก item ใน Library ก่อน");
      return;
    }
    const td = global.TableDesigner;
    if (!td) return;
    if (selectedKind === "headers") {
      td.addHeader(selectedId);
    } else if (selectedKind === "tables") {
      td.addConfigTable();
      const el = td.getSelectedElement && td.getSelectedElement();
      if (el) {
        el.presetId = selectedId;
        delete el.tablePreset;
        td.reflowElements();
        td.renderAll();
      }
    } else if (selectedKind === "fragments") {
      td.insertFragment(selectedId);
    }
  }

  async function saveFromSelection() {
    const td = global.TableDesigner;
    if (!td) return;
    if (selectedKind === "fragments") {
      await td.saveFragmentFromSelection();
      renderList();
      return;
    }
    const el = td.getSelectedElement && td.getSelectedElement();
    if (!el) {
      alert("เลือก element บน canvas ก่อน");
      return;
    }
    if (selectedKind === "headers") {
      await td.saveHeaderPresetFromElement(el);
      renderList();
      return;
    }
    if (selectedKind === "tables") {
      await td.saveTablePresetFromElement(el);
      renderList();
    }
  }

  function wire() {
    document.querySelectorAll(".side-tab").forEach((btn) => {
      btn.addEventListener("click", () => setSideTab(btn.getAttribute("data-side")));
    });
    document.querySelectorAll(".lib-subtab").forEach((btn) => {
      btn.addEventListener("click", () => setLibKind(btn.getAttribute("data-lib")));
    });
    const filter = $("libraryFilter");
    if (filter) {
      filter.addEventListener("input", () => {
        filterTag = filter.value.trim();
        renderList();
      });
    }
    const ins = $("btnLibInsert");
    if (ins) ins.addEventListener("click", () => insertSelected());
    const edit = $("btnLibEdit");
    if (edit) {
      edit.addEventListener("click", () => {
        if (!selectedId) {
          alert("เลือก Header ใน Library ก่อน");
          return;
        }
        if (selectedKind !== "headers") {
          alert("ตอนนี้ Edit on canvas รองรับเฉพาะ Headers");
          return;
        }
        openHeaderOnCanvas(selectedId);
      });
    }
    const save = $("btnLibSaveSelection");
    if (save) save.addEventListener("click", () => { saveFromSelection().catch((e) => alert(e.message || e)); });
  }

  global.LibraryStudio = {
    wire,
    refresh: renderList,
    setSideTab,
    setOpenHeader: function (fn) { openHeaderFn = fn; },
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", wire);
  } else {
    wire();
  }
})(window);
