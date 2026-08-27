/**
 * Port of HprpHeaderLayoutEngine (C#) — Studio HTML canvas for config-header.
 */
(function (global) {
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

  function buildLayout(preset, data, fallbackTitle) {
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

    const bottomFields = (preset.bottomFields || [])
      .filter((f) => !f.whenHdPerWeek || preset.showHdPerWeek)
      .map((line) => ({
        id: line.id,
        label: line.label || "",
        value: blank(readAt(data, line.bind)),
        weight: Math.max(0.1, Number(line.weight) || 1),
      }));

    return {
      preset,
      titleRowHeightMm: Number(preset.titleRowHeightMm) > 0 ? Number(preset.titleRowHeightMm) : 21.6,
      bottomRowHeightMm: Number(preset.bottomRowHeightMm) > 0 ? Number(preset.bottomRowHeightMm) : 5.4,
      titleText: title,
      logoBase64: logo || null,
      logoFallbackText: unitName,
      metaLines,
      bottomFields,
      showDateAndHdNo: !!preset.showDateAndHdNo,
      dateText: blank(readAt(data, "$.header.cycleStartTime")),
      hdNoText: blank(readAt(data, "$.header.treatmentNo")),
    };
  }

  /** Convert band columns to display fractions for a given content width (mm). */
  function bandFractions(columns, contentWMm) {
    const cols = columns || [];
    let fixed = 0;
    let weightSum = 0;
    cols.forEach((c) => {
      if (c.widthMm != null && Number(c.widthMm) > 0) fixed += Number(c.widthMm);
      else weightSum += Math.max(0.1, Number(c.weight) || 1);
    });
    const remain = Math.max(1, contentWMm - fixed);
    return cols.map((c) => {
      if (c.widthMm != null && Number(c.widthMm) > 0)
        return Number(c.widthMm) / contentWMm;
      const w = Math.max(0.1, Number(c.weight) || 1);
      return (remain * (w / (weightSum || 1))) / contentWMm;
    });
  }

  global.HeaderLayoutEngine = {
    buildLayout,
    bandFractions,
    readAt,
  };
})(window);
