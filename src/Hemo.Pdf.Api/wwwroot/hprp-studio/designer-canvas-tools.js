/**
 * HPRP Studio — canvas viewport tools: undo/redo, space-pan, scroll-zoom.
 */
(function (global) {
  const ZOOM_MIN = 0.35;
  const ZOOM_MAX = 2.5;
  const ZOOM_STEP = 0.08;
  const HISTORY_MAX = 80;

  function createCanvasTools(opts) {
    const getHost = opts.getHost;
    const getSnapshot = opts.getSnapshot;
    const applySnapshot = opts.applySnapshot;
    const onViewChanged = opts.onViewChanged;
    const isActive = opts.isActive || (() => true);

    let viewZoom = 1;
    let undoStack = [];
    let redoStack = [];
    let applying = false;
    let spaceHeld = false;
    let panning = false;
    let panStart = null;
    let wired = false;

    function clampZoom(z) {
      return Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, z));
    }

    function pushHistory() {
      if (applying || !isActive()) return;
      try {
        const snap = getSnapshot();
        if (!snap) return;
        const json = JSON.stringify(snap);
        if (undoStack.length && undoStack[undoStack.length - 1] === json) return;
        undoStack.push(json);
        if (undoStack.length > HISTORY_MAX) undoStack.shift();
        redoStack = [];
        updateButtons();
      } catch (_) { /* ignore */ }
    }

    function undo() {
      if (undoStack.length < 2) return;
      const current = undoStack.pop();
      redoStack.push(current);
      const prev = undoStack[undoStack.length - 1];
      applying = true;
      try {
        applySnapshot(JSON.parse(prev));
      } finally {
        applying = false;
      }
      updateButtons();
      onViewChanged();
    }

    function redo() {
      if (!redoStack.length) return;
      const next = redoStack.pop();
      undoStack.push(next);
      applying = true;
      try {
        applySnapshot(JSON.parse(next));
      } finally {
        applying = false;
      }
      updateButtons();
      onViewChanged();
    }

    function resetHistory() {
      undoStack = [];
      redoStack = [];
      pushHistory();
      updateButtons();
    }

    function setZoom(z, anchorClient) {
      const host = getHost();
      const prev = viewZoom;
      viewZoom = clampZoom(z);
      if (!host || viewZoom === prev) {
        updateButtons();
        onViewChanged();
        return;
      }
      const rect = host.getBoundingClientRect();
      const cx = anchorClient ? anchorClient.x - rect.left : host.clientWidth / 2;
      const cy = anchorClient ? anchorClient.y - rect.top : host.clientHeight / 2;
      const contentX = (host.scrollLeft + cx) / prev;
      const contentY = (host.scrollTop + cy) / prev;
      onViewChanged();
      host.scrollLeft = contentX * viewZoom - cx;
      host.scrollTop = contentY * viewZoom - cy;
      updateButtons();
    }

    function zoomBy(delta, anchor) {
      setZoom(viewZoom + delta, anchor);
    }

    function updateButtons() {
      const u = document.getElementById("btnCanvasUndo");
      const r = document.getElementById("btnCanvasRedo");
      const z = document.getElementById("canvasZoomLabel");
      if (u) u.disabled = undoStack.length < 2;
      if (r) r.disabled = redoStack.length === 0;
      if (z) z.textContent = Math.round(viewZoom * 100) + "%";
    }

    function setSpaceCursor(on) {
      const host = getHost();
      if (!host) return;
      host.classList.toggle("is-panning", !!on);
      host.style.cursor = on ? (panning ? "grabbing" : "grab") : "";
    }

    function onKeyDown(e) {
      if (!isActive()) return;
      const tag = (e.target && e.target.tagName) || "";
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || e.target.isContentEditable)
        return;

      if (e.code === "Space" && !e.repeat) {
        spaceHeld = true;
        setSpaceCursor(true);
        e.preventDefault();
        return;
      }

      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key.toLowerCase() === "z" && !e.shiftKey) {
        e.preventDefault();
        undo();
        return;
      }
      if (mod && (e.key.toLowerCase() === "y" || (e.key.toLowerCase() === "z" && e.shiftKey))) {
        e.preventDefault();
        redo();
      }
    }

    function onKeyUp(e) {
      if (e.code === "Space") {
        spaceHeld = false;
        panning = false;
        setSpaceCursor(false);
      }
    }

    function onPointerDown(e) {
      if (!isActive() || !spaceHeld || e.button !== 0) return;
      const host = getHost();
      if (!host || !host.contains(e.target)) return;
      panning = true;
      panStart = { x: e.clientX, y: e.clientY, sl: host.scrollLeft, st: host.scrollTop };
      setSpaceCursor(true);
      e.preventDefault();
    }

    function onPointerMove(e) {
      if (!panning || !panStart) return;
      const host = getHost();
      if (!host) return;
      host.scrollLeft = panStart.sl - (e.clientX - panStart.x);
      host.scrollTop = panStart.st - (e.clientY - panStart.y);
    }

    function onPointerUp() {
      if (!panning) return;
      panning = false;
      panStart = null;
      setSpaceCursor(spaceHeld);
    }

    function onWheel(e) {
      if (!isActive()) return;
      const host = getHost();
      if (!host || !host.contains(e.target)) return;
      // Scroll wheel zooms (design-tool style); space+drag pans.
      e.preventDefault();
      const dir = e.deltaY > 0 ? -ZOOM_STEP : ZOOM_STEP;
      zoomBy(dir, { x: e.clientX, y: e.clientY });
    }

    function wire() {
      if (wired) return;
      wired = true;
      window.addEventListener("keydown", onKeyDown, true);
      window.addEventListener("keyup", onKeyUp, true);
      document.addEventListener("pointerdown", onPointerDown, true);
      document.addEventListener("pointermove", onPointerMove, true);
      document.addEventListener("pointerup", onPointerUp, true);
      const host = getHost();
      if (host) host.addEventListener("wheel", onWheel, { passive: false });

      const undoBtn = document.getElementById("btnCanvasUndo");
      const redoBtn = document.getElementById("btnCanvasRedo");
      const zoomIn = document.getElementById("btnCanvasZoomIn");
      const zoomOut = document.getElementById("btnCanvasZoomOut");
      const zoomFit = document.getElementById("btnCanvasZoomFit");
      if (undoBtn) undoBtn.addEventListener("click", () => undo());
      if (redoBtn) redoBtn.addEventListener("click", () => redo());
      if (zoomIn) zoomIn.addEventListener("click", () => zoomBy(ZOOM_STEP));
      if (zoomOut) zoomOut.addEventListener("click", () => zoomBy(-ZOOM_STEP));
      if (zoomFit) zoomFit.addEventListener("click", () => setZoom(1));
      updateButtons();
    }

    return {
      wire,
      pushHistory,
      resetHistory,
      undo,
      redo,
      getZoom: () => viewZoom,
      setZoom,
      updateButtons,
    };
  }

  global.DesignerCanvasTools = { create: createCanvasTools };
})(window);
