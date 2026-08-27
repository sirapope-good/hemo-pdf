/**
 * HPRP Studio — canvas viewport tools: undo/redo, button / Ctrl± zoom.
 * (No space-pan, no scroll-zoom — scroll pans the host natively.)
 */
(function (global) {
  const ZOOM_MIN = 0.35;
  const ZOOM_MAX = 2.5;
  const ZOOM_STEP = 0.1;
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
    let wired = false;

    function clampZoom(z) {
      return Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, z));
    }

    function captureJson() {
      const snap = getSnapshot();
      if (!snap) return null;
      return JSON.stringify(snap);
    }

    function applyJson(json) {
      applying = true;
      try {
        applySnapshot(JSON.parse(json));
      } finally {
        applying = false;
      }
    }

    function trimUndo() {
      while (undoStack.length > HISTORY_MAX) undoStack.shift();
    }

    /**
     * Call BEFORE a mutating edit. Stores the current canvas state so Undo can return here.
     */
    function pushHistory() {
      if (applying || !isActive()) return;
      try {
        const json = captureJson();
        if (!json) return;
        if (undoStack.length && undoStack[undoStack.length - 1] === json) return;
        undoStack.push(json);
        trimUndo();
        redoStack = [];
        updateButtons();
      } catch (_) { /* ignore */ }
    }

    /**
     * Live state may be ahead of the stack top (edits after last checkpoint).
     * Sync live onto the stack before stepping undo/redo. Divergent live = new branch → drop redo.
     */
    function syncLiveOntoStack() {
      const live = captureJson();
      if (!live) return null;
      if (!undoStack.length || undoStack[undoStack.length - 1] !== live) {
        undoStack.push(live);
        trimUndo();
        redoStack = [];
      }
      return live;
    }

    function undo() {
      if (applying || !isActive()) return;
      try {
        syncLiveOntoStack();
        if (undoStack.length < 2) {
          updateButtons();
          return;
        }
        const current = undoStack.pop();
        redoStack.push(current);
        applyJson(undoStack[undoStack.length - 1]);
        updateButtons();
        onViewChanged();
      } catch (_) {
        updateButtons();
      }
    }

    function redo() {
      if (applying || !isActive()) return;
      try {
        const live = captureJson();
        // Branched after undo — discard redo future instead of stomping live edits.
        if (live && undoStack.length && live !== undoStack[undoStack.length - 1]) {
          undoStack.push(live);
          trimUndo();
          redoStack = [];
          updateButtons();
          return;
        }
        if (!redoStack.length) {
          updateButtons();
          return;
        }
        const next = redoStack.pop();
        undoStack.push(next);
        trimUndo();
        applyJson(next);
        updateButtons();
        onViewChanged();
      } catch (_) {
        updateButtons();
      }
    }

    function resetHistory() {
      undoStack = [];
      redoStack = [];
      applying = false;
      const json = captureJson();
      if (json) undoStack.push(json);
      updateButtons();
    }

    function setZoom(z) {
      const host = getHost();
      const prev = viewZoom;
      viewZoom = clampZoom(z);
      if (!host || viewZoom === prev) {
        updateButtons();
        onViewChanged();
        return;
      }
      const cx = host.clientWidth / 2;
      const cy = host.clientHeight / 2;
      const contentX = (host.scrollLeft + cx) / prev;
      const contentY = (host.scrollTop + cy) / prev;
      onViewChanged();
      host.scrollLeft = contentX * viewZoom - cx;
      host.scrollTop = contentY * viewZoom - cy;
      updateButtons();
    }

    function zoomBy(delta) {
      setZoom(viewZoom + delta);
    }

    function updateButtons() {
      const u = document.getElementById("btnCanvasUndo");
      const r = document.getElementById("btnCanvasRedo");
      const z = document.getElementById("canvasZoomLabel");
      try {
        const live = captureJson();
        const canUndo =
          undoStack.length >= 2
          || (undoStack.length === 1 && live && live !== undoStack[0]);
        if (u) u.disabled = !canUndo;
      } catch (_) {
        if (u) u.disabled = undoStack.length < 2;
      }
      if (r) r.disabled = redoStack.length === 0;
      if (z) z.textContent = Math.round(viewZoom * 100) + "%";
    }

    function onKeyDown(e) {
      if (!isActive()) return;
      const tag = (e.target && e.target.tagName) || "";
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || e.target.isContentEditable)
        return;

      const mod = e.ctrlKey || e.metaKey;
      if (!mod) return;

      const key = e.key;
      const code = e.code;

      if (key.toLowerCase() === "z" && !e.shiftKey) {
        e.preventDefault();
        undo();
        return;
      }
      if (key.toLowerCase() === "y" || (key.toLowerCase() === "z" && e.shiftKey)) {
        e.preventDefault();
        redo();
        return;
      }
      // Ctrl + + / =  and Ctrl + -
      if (key === "+" || key === "=" || code === "Equal" || code === "NumpadAdd") {
        e.preventDefault();
        zoomBy(ZOOM_STEP);
        return;
      }
      if (key === "-" || code === "Minus" || code === "NumpadSubtract") {
        e.preventDefault();
        zoomBy(-ZOOM_STEP);
        return;
      }
      // Ctrl + 0 → 100%
      if (key === "0" || code === "Digit0" || code === "Numpad0") {
        e.preventDefault();
        setZoom(1);
      }
    }

    function wire() {
      if (wired) return;
      wired = true;
      window.addEventListener("keydown", onKeyDown, true);

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
