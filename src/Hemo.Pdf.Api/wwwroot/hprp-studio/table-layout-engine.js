/**
 * Port of HprpTableLayoutEngine (C#) — shared rules for Studio HTML canvas.
 */
(function (global) {
  const ROW_ANNUAL = "annual";
  const ROW_MONTHLY = "monthly";
  const ROW_FREEDOM = "freedom";
  const ROW_MATRIX = "matrix";
  const CTX_ENTRY = "entry";
  const CTX_GROUP = "group-label";
  const CTX_FREEDOM = "freedom-row";
  const CTX_LAB = "lab-historical";
  const THAI_MONTHS = ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];

  function mergeColumns(baseCols, overrides) {
    if (!overrides || !overrides.length) return (baseCols || []).slice();
    const map = {};
    (baseCols || []).forEach((c) => { map[c.id] = { ...c }; });
    overrides.forEach((o) => { if (o.id) map[o.id] = { ...map[o.id], ...o }; });
    const ordered = (baseCols || []).map((c) => map[c.id]);
    overrides.forEach((o) => {
      if (o.id && !(baseCols || []).some((b) => b.id === o.id)) ordered.push(map[o.id]);
    });
    return ordered;
  }

  function resolvePreset(preset, element) {
    const date = preset.dateColumns || { monthWeight: 0.45, dayWeight: 1.35 };
    return {
      id: preset.id,
      rowMode: preset.rowMode || ROW_ANNUAL,
      groupCount: preset.groupCount || 12,
      slotsPerGroup: preset.slotsPerGroup || 3,
      freedomRowCount: preset.freedomRowCount || 10,
      dateColumns: date,
      columns: mergeColumns(preset.columns, element && element.columnOverrides),
      staticRows: preset.staticRows || null,
      chrome: (element && element.chrome) || preset.chrome || {},
    };
  }

  function readAt(data, path, groupIndex, slotIndex) {
    if (!data || !path) return null;
    const parts = path.split(".").filter(Boolean);
    let cur = data;
    for (let i = 0; i < parts.length; i++) {
      let seg = parts[i];
      const wild = seg.endsWith("[]");
      if (wild) seg = seg.slice(0, -2);
      if (cur == null || typeof cur !== "object") return null;
      if (Array.isArray(cur)) {
        const idx = seg.includes("entries") ? slotIndex : groupIndex;
        cur = cur[idx];
        continue;
      }
      if (!(seg in cur)) return null;
      cur = cur[seg];
      if (wild && Array.isArray(cur)) {
        const idx = seg.includes("entries") ? slotIndex : groupIndex;
        cur = cur[idx];
      }
    }
    if (cur == null) return null;
    if (typeof cur === "boolean") return cur ? "true" : "false";
    return String(cur);
  }

  function readBool(data, path, g, s) {
    return readAt(data, path, g, s) === "true";
  }

  function label(labels, key, fallback) {
    if (!labels || !key) return fallback || "";
    return labels[key] || fallback || key;
  }

  function freedomRowCount(p) {
    if (p.staticRows && p.staticRows.length) return p.staticRows.length;
    return Math.max(1, p.freedomRowCount || 1);
  }

  function countMatrixVisualRows(data) {
    const items = data && Array.isArray(data.checklistItems) ? data.checklistItems : [];
    if (!items.length) return 8;
    let count = 0;
    let lastGroup = null;
    items.forEach((item) => {
      const group = item && item.group;
      if (group && group !== lastGroup) {
        lastGroup = group;
        count++;
      }
      count++;
    });
    return Math.max(1, count);
  }

  function budgetSlotHeight(boxHeightMm, rowMode, groupCount, slotsPerGroup, freeRows, matrixRows) {
    const headerMm = rowMode === ROW_MATRIX ? 14 : 5;
    const available = Math.max(0, boxHeightMm - headerMm);
    if (rowMode === ROW_FREEDOM) {
      const rows = Math.max(1, freeRows || slotsPerGroup || 1);
      return Math.max(available / rows, 4);
    }
    if (rowMode === ROW_MATRIX) {
      const rows = Math.max(1, matrixRows || slotsPerGroup || 1);
      return Math.max(available / rows, 4);
    }
    const groups = Math.max(1, groupCount);
    const perBlock = available / groups;
    return Math.max(perBlock / Math.max(1, slotsPerGroup), 4);
  }

  function resolveBinding(bindings, data, g, s, column, context) {
    if (!data || !bindings) return null;
    for (let i = 0; i < bindings.length; i++) {
      const b = bindings[i];
      if (b.column !== column || b.context !== context) continue;
      return readAt(data, b.path, g, s);
    }
    return null;
  }

  function buildLayout(preset, element, labels, data, boxHeightMm) {
    const p = resolvePreset(preset, element);
    const bindings = (element && element.bindings) || [];
    const rowMode = (p.rowMode || ROW_ANNUAL).toLowerCase();
    const freeRows = freedomRowCount(p);
    const matrixRows = rowMode === ROW_MATRIX ? countMatrixVisualRows(data) : 0;
    const slots = rowMode === ROW_FREEDOM
      ? freeRows
      : (rowMode === ROW_MATRIX ? Math.max(1, matrixRows) : Math.max(1, p.slotsPerGroup));
    const slotHeight = budgetSlotHeight(boxHeightMm, rowMode, p.groupCount, slots, freeRows, matrixRows);

    const headerLabels = (rowMode === ROW_FREEDOM || rowMode === ROW_MATRIX)
      ? p.columns.map((c) => label(labels, c.labelKey, c.title || c.id))
      : [label(labels, p.dateColumns.dateHeaderLabelKey || "colDate", "วัน/เดือน/ปี")]
          .concat(p.columns.map((c) => label(labels, c.labelKey, c.title || c.id)));

    const rows = [];
    if (rowMode === ROW_MATRIX) {
      const items = data && Array.isArray(data.checklistItems) ? data.checklistItems : [];
      const monthCount = data && Array.isArray(data.columns) ? data.columns.length : 0;
      let lastGroup = null;
      items.forEach((item, index) => {
        const group = item && item.group;
        if (group && group !== lastGroup) {
          lastGroup = group;
          rows.push({
            kind: "group",
            groupIndex: index,
            slotIndex: 0,
            groupLabel: group,
            cells: [{ text: group, historical: false, center: false }],
          });
        }
        const cells = [{ text: (item && item.label) || " ", historical: false, center: false }];
        const marks = (item && Array.isArray(item.marks)) ? item.marks : [];
        for (let m = 0; m < Math.max(monthCount, marks.length); m++) {
          cells.push({
            text: marks[m] != null && String(marks[m]) !== "" ? String(marks[m]) : " ",
            historical: false,
            center: true,
          });
        }
        rows.push({ kind: "matrix", groupIndex: index, slotIndex: 0, groupLabel: null, cells });
      });
    } else if (rowMode === ROW_FREEDOM) {
      if (p.staticRows && p.staticRows.length) {
        p.staticRows.forEach((src, r) => {
          const cells = p.columns.map((col, ci) => ({
            text: (src && src[ci] != null && String(src[ci]) !== "") ? String(src[ci]) : " ",
            historical: false,
            center: !!col.center,
          }));
          rows.push({ kind: "freedom", groupIndex: 0, slotIndex: r, groupLabel: null, cells });
        });
      } else {
        for (let r = 0; r < freeRows; r++) {
          const cells = p.columns.map((col) => ({
            text: resolveBinding(bindings, data, r, 0, col.id, CTX_FREEDOM) || " ",
            historical: false,
            center: !!col.center,
          }));
          rows.push({ kind: "freedom", groupIndex: 0, slotIndex: r, groupLabel: null, cells });
        }
      }
    } else {
      const groups = Math.max(1, p.groupCount);
      for (let g = 0; g < groups; g++) {
        const groupLabel =
          resolveBinding(bindings, data, g, 0, "month", CTX_GROUP)
          || (g < THAI_MONTHS.length ? THAI_MONTHS[g] : String(g + 1));
        for (let s = 0; s < Math.max(1, p.slotsPerGroup); s++) {
          const historical = bindings.some((b) => b.context === CTX_LAB && readBool(data, b.path, g, s))
            || readBool(data, "months[].entries[].labIsHistorical", g, s);
          const cells = [{
            text: resolveBinding(bindings, data, g, s, "day", CTX_ENTRY) || " ",
            historical,
            center: true,
          }];
          p.columns.forEach((col) => {
            cells.push({
              text: resolveBinding(bindings, data, g, s, col.id, CTX_ENTRY) || " ",
              historical: col.isLab && historical,
              center: !!col.center,
            });
          });
          rows.push({
            kind: "entry",
            groupIndex: g,
            slotIndex: s,
            groupLabel: s === 0 ? groupLabel : null,
            cells,
          });
        }
      }
    }

    return {
      preset: p,
      headerHeightMm: 5,
      slotHeightMm: slotHeight,
      blockHeightMm: slotHeight * (rowMode === ROW_FREEDOM || rowMode === ROW_MATRIX
        ? Math.max(1, rows.length || slots)
        : Math.max(1, p.slotsPerGroup)),
      headerLabels,
      rows,
    };
  }

  global.TableLayoutEngine = {
    mergeColumns,
    resolvePreset,
    buildLayout,
    budgetSlotHeight,
    ROW_ANNUAL,
    ROW_MONTHLY,
    ROW_FREEDOM,
    ROW_MATRIX,
  };
})(window);
