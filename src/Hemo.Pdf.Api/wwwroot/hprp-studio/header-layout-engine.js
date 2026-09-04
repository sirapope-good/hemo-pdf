/**
 * Port of HprpHeaderLayoutEngine (C#) — Studio HTML canvas for config-header.
 */
(function (global) {
  const BOTTOM_DIAGNOSIS = "diagnosis";
  const BOTTOM_CHECKLIST = "checklist-patient";
  const BOTTOM_NONE = "none";

  function readAt(data, path) {
    if (!data || !path) return null;
    let p = String(path).trim();
    if (p.startsWith("$.")) p = p.slice(2);
    else if (p.startsWith("$")) p = p.slice(1).replace(/^\./, "");
    const parts = p.split(".").filter(Boolean);
    let cur = data;
    for (let i = 0; i < parts.length; i++) {
      if (cur == null || typeof cur !== "object") return null;
      cur = cur[parts[i]];
    }
    if (cur == null) return null;
    if (Array.isArray(cur)) {
      return cur.filter((x) => x != null && String(x).trim() !== "").map(String).join(", ") || null;
    }
    if (typeof cur === "boolean") return cur ? "true" : "false";
    return String(cur);
  }

  function blank(v) {
    return v == null || String(v).trim() === "" ? "" : String(v).trim();
  }

  function resolveBottomMode(preset, bottomModeOverride) {
    const raw = (bottomModeOverride != null && String(bottomModeOverride).trim() !== "")
      ? String(bottomModeOverride)
      : (preset && preset.bottomMode) || BOTTOM_DIAGNOSIS;
    const mode = String(raw).trim().toLowerCase();
    if (mode === BOTTOM_CHECKLIST || mode === BOTTOM_NONE || mode === BOTTOM_DIAGNOSIS) return mode;
    return BOTTOM_DIAGNOSIS;
  }

  function resolveBottomSource(preset, mode) {
    if (mode === BOTTOM_NONE) return { heightMm: 0, fields: [] };
    const sets = preset && preset.bottomFieldSets;
    if (sets) {
      const key = Object.keys(sets).find((k) => String(k).toLowerCase() === mode);
      if (key && sets[key]) {
        const set = sets[key];
        return {
          heightMm: Number(set.heightMm) > 0
            ? Number(set.heightMm)
            : (Number(preset.bottomRowHeightMm) > 0 ? Number(preset.bottomRowHeightMm) : 5.4),
          fields: set.fields || [],
        };
      }
    }
    if (mode === BOTTOM_DIAGNOSIS && preset && preset.bottomFields && preset.bottomFields.length) {
      return {
        heightMm: Number(preset.bottomRowHeightMm) > 0 ? Number(preset.bottomRowHeightMm) : 5.4,
        fields: preset.bottomFields,
      };
    }
    return { heightMm: 0, fields: [] };
  }

  function buildLayout(preset, data, fallbackTitle, bottomModeOverride) {
    const cols = preset.columns || [];
    const titleCol = cols.find((c) => String(c.kind || "").toLowerCase() === "title");
    const logoCol = cols.find((c) => String(c.kind || "").toLowerCase() === "logo");
    const title = blank(readAt(data, (titleCol && titleCol.bind) || "$.title"))
      || fallbackTitle
      || "";
    const logo = blank(readAt(data, (logoCol && logoCol.bind) || "$.header.logoBase64"));
    const unitName = blank(readAt(data, "$.header.unit.fullName"));

    const metaLines = (preset.metaLines || []).map((line) => ({
      id: line.id,
      label: line.label || "",
      value: blank(readAt(data, line.bind)),
      label2: line.label2 || null,
      value2: line.bind2 ? blank(readAt(data, line.bind2)) : null,
      weight: Number(line.weight) || 1,
    }));

    const mode = resolveBottomMode(preset, bottomModeOverride);
    const bottomSrc = resolveBottomSource(preset, mode);
    const bottomFields = (bottomSrc.fields || [])
      .filter((f) => !f.whenHdPerWeek || preset.showHdPerWeek)
      .map((line) => ({
        id: line.id,
        label: line.label || "",
        value: blank(readAt(data, line.bind)),
        weight: Math.max(0.1, Number(line.weight) || 1),
        row: Math.max(0, Number(line.row) || 0),
      }));
    const bottomRowCount = bottomFields.length
      ? Math.max(1, ...bottomFields.map((f) => f.row + 1))
      : 0;

    return {
      preset,
      titleRowHeightMm: Number(preset.titleRowHeightMm) > 0 ? Number(preset.titleRowHeightMm) : 21.6,
      bottomRowHeightMm: bottomSrc.heightMm,
      titleText: title,
      logoBase64: logo || null,
      logoFallbackText: unitName,
      metaLines,
      bottomFields,
      bottomMode: mode,
      bottomRowCount,
      showDateAndHdNo: !!preset.showDateAndHdNo,
      dateText: blank(readAt(data, "$.header.cycleStartTime")),
      hdNoText: blank(readAt(data, "$.header.treatmentNo")),
    };
  }

  /** Convert band columns to display fractions for a given content width (mm).
   * Fixed mm columns scale with content width so landscape stays proportional to portrait (design base 206). */
  function bandFractions(columns, contentWMm) {
    const cols = columns || [];
    const designW = 206;
    const w = Math.max(1, Number(contentWMm) || designW);
    const scaleFixed = w / designW;
    let fixed = 0;
    let weightSum = 0;
    cols.forEach((c) => {
      if (c.widthMm != null && Number(c.widthMm) > 0) fixed += Number(c.widthMm) * scaleFixed;
      else weightSum += Math.max(0.1, Number(c.weight) || 1);
    });
    const remain = Math.max(1, w - fixed);
    return cols.map((c) => {
      if (c.widthMm != null && Number(c.widthMm) > 0)
        return (Number(c.widthMm) * scaleFixed) / w;
      const weight = Math.max(0.1, Number(c.weight) || 1);
      return (remain * (weight / (weightSum || 1))) / w;
    });
  }

  global.HeaderLayoutEngine = {
    buildLayout,
    bandFractions,
    readAt,
    resolveBottomMode,
    BOTTOM_DIAGNOSIS,
    BOTTOM_CHECKLIST,
    BOTTOM_NONE,
  };
})(window);
