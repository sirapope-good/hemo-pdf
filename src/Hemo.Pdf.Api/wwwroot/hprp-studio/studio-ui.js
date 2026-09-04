/**
 * HPRP Studio — shared chrome: dropdown menus, brand loader, list/canvas skeletons.
 */
(function (global) {
  const MENU_SELECTOR = ".tb-menu";
  let busyDepth = 0;
  let overlayEl = null;

  function ensureOverlay() {
    if (overlayEl) return overlayEl;
    overlayEl = document.getElementById("studioBusyOverlay");
    if (overlayEl) return overlayEl;
    overlayEl = document.createElement("div");
    overlayEl.id = "studioBusyOverlay";
    overlayEl.className = "studio-busy-overlay hidden";
    overlayEl.setAttribute("aria-hidden", "true");
    overlayEl.innerHTML =
      '<div class="studio-busy-card" role="status" aria-live="polite">' +
      '<div class="studio-spinner" aria-hidden="true"></div>' +
      '<span class="studio-busy-label">Loading…</span>' +
      "</div>";
    document.body.appendChild(overlayEl);
    return overlayEl;
  }

  function showBusy(message) {
    busyDepth += 1;
    const el = ensureOverlay();
    const label = el.querySelector(".studio-busy-label");
    if (label) label.textContent = message || "Loading…";
    el.classList.remove("hidden");
    el.setAttribute("aria-hidden", "false");
  }

  function hideBusy() {
    busyDepth = Math.max(0, busyDepth - 1);
    if (busyDepth > 0) return;
    const el = ensureOverlay();
    el.classList.add("hidden");
    el.setAttribute("aria-hidden", "true");
  }

  /** Run async work with brand overlay. Always clears even on throw. */
  async function withBusy(message, work) {
    showBusy(message);
    try {
      return await work();
    } finally {
      hideBusy();
    }
  }

  function skeletonRows(count) {
    const n = Math.max(1, count | 0);
    let html = "";
    for (let i = 0; i < n; i++) {
      html +=
        '<li class="studio-skel-row" aria-hidden="true">' +
        '<span class="studio-skel studio-skel-title"></span>' +
        '<span class="studio-skel studio-skel-meta"></span>' +
        "</li>";
    }
    return html;
  }

  function showListSkeleton(listEl, count) {
    if (!listEl) return;
    listEl.innerHTML = skeletonRows(count || 5);
    listEl.classList.add("is-loading");
  }

  function clearListLoading(listEl) {
    if (!listEl) return;
    listEl.classList.remove("is-loading");
  }

  function showCanvasSkeleton() {
    const host = document.getElementById("designerCanvas");
    if (!host) return;
    host.innerHTML =
      '<div class="studio-canvas-skel" aria-busy="true" aria-label="Loading canvas">' +
      '<div class="studio-skel-sheet">' +
      '<div class="studio-skel studio-skel-band"></div>' +
      '<div class="studio-skel studio-skel-block"></div>' +
      '<div class="studio-skel studio-skel-block short"></div>' +
      '<div class="studio-skel studio-skel-block"></div>' +
      "</div></div>";
  }

  function closeAllMenus(except) {
    document.querySelectorAll(MENU_SELECTOR).forEach((menu) => {
      if (except && menu === except) return;
      menu.classList.remove("open");
      const btn = menu.querySelector(".tb-menu-btn");
      if (btn) btn.setAttribute("aria-expanded", "false");
    });
  }

  function wireMenus() {
    document.querySelectorAll(MENU_SELECTOR).forEach((menu) => {
      const btn = menu.querySelector(".tb-menu-btn");
      if (!btn || btn.dataset.menuWired === "1") return;
      btn.dataset.menuWired = "1";
      btn.setAttribute("aria-haspopup", "true");
      btn.setAttribute("aria-expanded", "false");
      btn.addEventListener("click", (ev) => {
        ev.preventDefault();
        ev.stopPropagation();
        const willOpen = !menu.classList.contains("open");
        closeAllMenus();
        if (willOpen) {
          menu.classList.add("open");
          btn.setAttribute("aria-expanded", "true");
        }
      });
      menu.querySelectorAll(".tb-menu-panel button").forEach((item) => {
        item.addEventListener("click", () => closeAllMenus());
      });
    });

    if (!document.body.dataset.studioMenusWired) {
      document.body.dataset.studioMenusWired = "1";
      document.addEventListener("click", () => closeAllMenus());
      document.addEventListener("keydown", (ev) => {
        if (ev.key === "Escape") closeAllMenus();
      });
    }
  }

  function init() {
    ensureOverlay();
    wireMenus();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }

  global.StudioUi = {
    init,
    wireMenus,
    showBusy,
    hideBusy,
    withBusy,
    showListSkeleton,
    clearListLoading,
    showCanvasSkeleton,
    closeAllMenus,
  };
})(window);
