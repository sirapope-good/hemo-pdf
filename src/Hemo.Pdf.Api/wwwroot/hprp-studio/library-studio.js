/**
 * HPRP Studio — left pane Library tab (headers / tables / fragments).
 */
(function (global) {
  let selectedKind = "headers";
  let selectedId = null;
  let filterTag = "";
  /** Set by studio.js — open handlers per kind. */
  let openHandlers = {
    headers: null,
    tables: null,
    fragments: null,
  };

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

  function openOnCanvas(kind, presetId) {
    const fn = openHandlers[kind]
      || (kind === "headers" ? global.openLibraryHeader : null)
      || (kind === "tables" ? global.openLibraryTable : null)
      || (kind === "fragments" ? global.openLibraryFragment : null);
    if (typeof fn === "function") {
      fn(presetId);
      return;
    }
    console.error("[LibraryStudio] open handler missing for", kind);
    alert("ยังโหลดตัวแก้ Library ไม่ครบ — กด Ctrl+F5 แล้วลองอีกครั้ง");
  }

  function renderList() {
    const list = $("libraryList");
    if (!list) return;
    list.innerHTML = "";
    if (window.StudioUi) StudioUi.clearListLoading(list);
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
      li.title = "คลิกเพื่อแก้บน canvas";
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
        list.querySelectorAll(".lib-item").forEach((node) => {
          node.classList.toggle("active", node.dataset.id === item.id);
        });
        openOnCanvas(selectedKind, item.id);
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

  async function deleteSelected() {
    if (!selectedId) {
      alert("เลือก item ใน Library ก่อน");
      return;
    }
    const id = selectedId;
    const kind = selectedKind;
    const folder = kind === "headers" ? "headers" : kind === "tables" ? "tables" : "fragments";
    if (!confirm(
      "ลบ library " + kind.slice(0, -1) + " \"" + id + "\"?\n\n"
      + "ลบได้เฉพาะไฟล์ใน packages/library/" + folder + "/\n"
      + "ถ้ามี seed ใน assets จะกลับไปใช้ seed"
    )) {
      return;
    }
    const td = global.TableDesigner;
    if (!td) return;
    const delFn = kind === "headers"
      ? td.deleteLibraryHeader
      : kind === "tables"
        ? td.deleteLibraryTable
        : td.deleteLibraryFragment;
    if (typeof delFn !== "function") {
      alert("Delete handler ไม่พร้อม — hard refresh (Ctrl+F5) และ rebuild API");
      return;
    }
    try {
      const result = await delFn.call(td, id);
      selectedId = null;
      renderList();
      const msg = (result && result.message) || ("Deleted " + id);
      const status = document.getElementById("status");
      if (status) {
        status.textContent = msg;
        status.className = "status ok";
      }
      const title = document.getElementById("editorTitle");
      if (title && String(title.textContent || "").indexOf(id) >= 0) {
        title.textContent = "Library item deleted";
      }
    } catch (e) {
      alert(e.message || String(e));
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
          alert("เลือก item ใน Library ก่อน");
          return;
        }
        openOnCanvas(selectedKind, selectedId);
      });
    }
    const del = $("btnLibDelete");
    if (del) del.addEventListener("click", () => { deleteSelected().catch((e) => alert(e.message || e)); });
    const save = $("btnLibSaveSelection");
    if (save) save.addEventListener("click", () => { saveFromSelection().catch((e) => alert(e.message || e)); });
  }

  global.LibraryStudio = {
    wire,
    refresh: renderList,
    setSideTab,
    setOpenHeader: function (fn) { openHandlers.headers = fn; },
    setOpenHandlers: function (map) {
      if (!map) return;
      if (map.headers) openHandlers.headers = map.headers;
      if (map.tables) openHandlers.tables = map.tables;
      if (map.fragments) openHandlers.fragments = map.fragments;
    },
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", wire);
  } else {
    wire();
  }
})(window);
