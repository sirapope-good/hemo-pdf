/**
 * Absolute layoutMode designer (experimental).
 * Coordinates are millimetres; PDF uses the same values via QuestPDF Layers.
 * Loaded after studio.js — uses shared `state`, `els`, `api`, `setStatus`, `schedulePreview`, `pretty`.
 */
(function () {
  const A4_W_MM = 210;
  const A4_H_MM = 297;
  const DISPLAY_W_PX = 420;
  const SNAP_MM = 2;

  let selectedId = null;
  let drag = null;

  function isAbsoluteMode() {
    return String((state.draft.manifest && state.draft.manifest.layoutMode) || "")
      .toLowerCase() === "absolute";
  }

  function ensureWidgets() {
    if (!state.draft.layout || typeof state.draft.layout !== "object")
      state.draft.layout = { page: { size: "A4" }, widgets: [] };
    if (!Array.isArray(state.draft.layout.widgets))
      state.draft.layout.widgets = [];
    if (!state.draft.layout.page)
      state.draft.layout.page = { size: "A4", orientation: "portrait", marginMm: 8 };
  }

  function landscape() {
    return String((state.draft.layout.page && state.draft.layout.page.orientation) || "")
      .toLowerCase() === "landscape";
  }

  function pageSizeMm() {
    return landscape()
      ? { w: A4_H_MM, h: A4_W_MM }
      : { w: A4_W_MM, h: A4_H_MM };
  }

  function mmPerPx() {
    return pageSizeMm().w / DISPLAY_W_PX;
  }

  function snapMm(v) {
    return Math.round(v / SNAP_MM) * SNAP_MM;
  }

  function widgets() {
    ensureWidgets();
    return state.draft.layout.widgets;
  }

  function syncBodyClass() {
    document.body.classList.toggle("mode-absolute", isAbsoluteMode());
    document.body.classList.toggle("mode-composition", !isAbsoluteMode() && state.mode === "designer");
  }

  function createWidget(type, options) {
    ensureWidgets();
    const id = "w_" + Math.random().toString(36).slice(2, 9);
    const opts = options || {};
    const base = {
      id,
      type,
      xMm: 10,
      yMm: 10,
      wMm: 60,
      hMm: 25,
      zIndex: widgets().length + 1,
      style: { backgroundColor: "#ffffff", borderColor: "#cbd5e1", borderWidth: 0.4 },
      data: {},
    };
    if (type === "text") {
      base.wMm = 90;
      base.hMm = 28;
      base.style.backgroundColor = "transparent";
      base.style.borderWidth = 0;
      base.data = { title: "Title", content: "Edit me", style: "body" };
    } else if (type === "frame") {
      base.wMm = 180;
      base.hMm = 40;
      base.style.backgroundColor = "transparent";
      base.style.borderWidth = 0.6;
      base.data = { label: "Frame" };
    } else if (type === "table") {
      base.wMm = 180;
      base.hMm = 45;
      base.data = {
        headers: ["A", "B", "C"],
        rows: [["1", "2", "3"], ["4", "5", "6"]],
      };
    } else if (type === "dense" && opts.widget) {
      base.widget = opts.widget;
      base.wMm = opts.wMm || 190;
      base.hMm = opts.hMm || 40;
      base.style = { backgroundColor: "transparent", borderWidth: 0 };
      if (opts.chrome) base.chrome = opts.chrome;
    }
    widgets().push(base);
    selectedId = id;
    renderAbsoluteDesigner();
    schedulePreview();
  }

  const CLINICAL_01_DENSE = [
    { widget: "thaiur.header", label: "ThaiUR header", wMm: 206, hMm: 27 },
    {
      widget: "clinical.hct-epo-annual-table",
      label: "Hct/EPO annual table",
      wMm: 206,
      hMm: 180,
      chrome: { headerFill: "$branding.sectionHeaderBackground", border: "thin" },
    },
    {
      widget: "clinical.hct-epo-copay",
      label: "Hct/EPO co-pay",
      wMm: 206,
      hMm: 34,
      chrome: { headerFill: "$branding.sectionHeaderBackground", border: "thin" },
    },
  ];

  function renderAbsolutePalette() {
    const host = document.getElementById("absolutePalette");
    if (!host) return;
    host.innerHTML = "";
    const group = document.createElement("div");
    group.className = "group";
    group.textContent = "Absolute widgets (mm)";
    host.appendChild(group);
    [["text", "Text"], ["frame", "Frame"], ["table", "Table"]].forEach(([type, label]) => {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.innerHTML = `<strong>${label}</strong><span class="pid">${type}</span>`;
      btn.addEventListener("click", () => createWidget(type));
      host.appendChild(btn);
    });

    const denseGroup = document.createElement("div");
    denseGroup.className = "group";
    denseGroup.textContent = "Clinical-01 dense (reusable)";
    host.appendChild(denseGroup);
    CLINICAL_01_DENSE.forEach((item) => {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.innerHTML = `<strong>${item.label}</strong><span class="pid">${item.widget}</span>`;
      btn.addEventListener("click", () =>
        createWidget("dense", {
          widget: item.widget,
          wMm: item.wMm,
          hMm: item.hMm,
          chrome: item.chrome,
        }));
      host.appendChild(btn);
    });
  }

  function renderAbsoluteSheet() {
    const host = document.getElementById("absoluteSheet");
    if (!host) return;
    host.innerHTML = "";
    const size = pageSizeMm();
    const scale = DISPLAY_W_PX / size.w;
    const sheet = document.createElement("div");
    sheet.className = "abs-sheet " + (landscape() ? "landscape" : "portrait");
    sheet.style.width = DISPLAY_W_PX + "px";
    sheet.style.height = (size.h * scale) + "px";

    const label = document.createElement("div");
    label.className = "page-sheet-label";
    label.textContent = `A4 ${landscape() ? "landscape" : "portrait"} · absolute mm · QuestPDF`;
    sheet.appendChild(label);

    widgets().slice().sort((a, b) => (a.zIndex || 0) - (b.zIndex || 0)).forEach((w) => {
      const el = document.createElement("div");
      el.className = "abs-widget" + (w.id === selectedId ? " selected" : "");
      el.style.left = (w.xMm * scale) + "px";
      el.style.top = (w.yMm * scale) + "px";
      el.style.width = (w.wMm * scale) + "px";
      el.style.height = (w.hMm * scale) + "px";
      el.style.zIndex = String(w.zIndex || 1);
      el.innerHTML = `<div class="abs-widget-title">${w.type === "dense" ? (w.widget || "dense") : w.type}</div><div class="abs-widget-meta">${w.xMm},${w.yMm} · ${w.wMm}×${w.hMm} mm</div>`;
      const handle = document.createElement("div");
      handle.className = "abs-resize";
      el.appendChild(handle);

      el.addEventListener("mousedown", (e) => {
        if (e.target === handle) {
          selectedId = w.id;
          drag = {
            mode: "resize",
            id: w.id,
            startX: e.clientX,
            startY: e.clientY,
            xMm: w.xMm,
            yMm: w.yMm,
            wMm: w.wMm,
            hMm: w.hMm,
          };
          e.preventDefault();
          return;
        }
        selectedId = w.id;
        drag = {
          mode: "move",
          id: w.id,
          startX: e.clientX,
          startY: e.clientY,
          xMm: w.xMm,
          yMm: w.yMm,
          wMm: w.wMm,
          hMm: w.hMm,
        };
        renderAbsoluteInspector();
        e.preventDefault();
      });
      sheet.appendChild(el);
    });

    host.appendChild(sheet);
  }

  function renderAbsoluteInspector() {
    const insp = els.inspector;
    if (!insp || !isAbsoluteMode()) return;
    insp.innerHTML = "";
    const w = widgets().find((x) => x.id === selectedId);
    if (!w) {
      insp.innerHTML = `<p class="muted">เลือก widget บน Absolute canvas หรือเพิ่มจาก palette</p>`;
      return;
    }
    const head = document.createElement("p");
    head.innerHTML = `<strong>${w.type === "dense" ? (w.widget || "dense") : w.type}</strong> <span class="muted">${w.id}</span>`;
    insp.appendChild(head);

    const note = document.createElement("p");
    note.className = "inspector-dense-note";
    note.textContent = w.type === "dense"
      ? "Dense C# widget ในกล่อง mm — ปรับขนาดกล่องได้; ภายในยังเป็น section composer เดิม (chrome/columnPlan override ได้ใน JSON)"
      : "พิกัด mm ส่งตรงเข้า QuestPDF Layers — Preview/Download คือ PDF จริงของโหมด absolute";
    insp.appendChild(note);

    function numField(label, value, apply) {
      const input = document.createElement("input");
      input.type = "number";
      input.step = "1";
      input.value = String(value);
      input.addEventListener("change", () => {
        apply(Number(input.value));
        renderAbsoluteDesigner();
        schedulePreview();
      });
      const wrap = document.createElement("label");
      wrap.textContent = label;
      wrap.appendChild(input);
      insp.appendChild(wrap);
    }

    numField("xMm", w.xMm, (v) => { w.xMm = snapMm(Math.max(0, v)); });
    numField("yMm", w.yMm, (v) => { w.yMm = snapMm(Math.max(0, v)); });
    numField("wMm", w.wMm, (v) => { w.wMm = Math.max(5, snapMm(v)); });
    numField("hMm", w.hMm, (v) => { w.hMm = Math.max(5, snapMm(v)); });
    numField("zIndex", w.zIndex || 1, (v) => { w.zIndex = v || 1; });

    if (w.type === "text") {
      ["title", "content"].forEach((key) => {
        const input = document.createElement("input");
        input.type = "text";
        input.value = (w.data && w.data[key]) || "";
        input.addEventListener("change", () => {
          w.data = w.data || {};
          w.data[key] = input.value;
          schedulePreview();
        });
        const wrap = document.createElement("label");
        wrap.textContent = key;
        wrap.appendChild(input);
        insp.appendChild(wrap);
      });
    }

    const del = document.createElement("button");
    del.type = "button";
    del.textContent = "Delete widget";
    del.addEventListener("click", () => {
      state.draft.layout.widgets = widgets().filter((x) => x.id !== w.id);
      selectedId = null;
      renderAbsoluteDesigner();
      schedulePreview();
    });
    insp.appendChild(del);
  }

  function renderAbsoluteDesigner() {
    if (!isAbsoluteMode()) return;
    syncBodyClass();
    ensureWidgets();
    renderAbsolutePalette();
    renderAbsoluteSheet();
    renderAbsoluteInspector();
  }

  window.addEventListener("mousemove", (e) => {
    if (!drag || !isAbsoluteMode()) return;
    const w = widgets().find((x) => x.id === drag.id);
    if (!w) return;
    const scale = 1 / mmPerPx();
    const dx = (e.clientX - drag.startX) / scale;
    const dy = (e.clientY - drag.startY) / scale;
    if (drag.mode === "move") {
      w.xMm = snapMm(Math.max(0, drag.xMm + dx));
      w.yMm = snapMm(Math.max(0, drag.yMm + dy));
    } else {
      w.wMm = Math.max(5, snapMm(drag.wMm + dx));
      w.hMm = Math.max(5, snapMm(drag.hMm + dy));
    }
    renderAbsoluteSheet();
    renderAbsoluteInspector();
  });

  window.addEventListener("mouseup", () => {
    if (!drag) return;
    drag = null;
    schedulePreview();
  });

  window.AbsoluteDesigner = {
    isAbsoluteMode,
    renderAbsoluteDesigner,
    syncBodyClass,
    ensureWidgets,
  };
})();
