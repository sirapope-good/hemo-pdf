# Hemopro Report Package (`.hprp`)

Composition documents for Hemo-PDF. The engine (QuestPDF widgets, `ReportBlock` types, data adapters) stays in C#. `.hprp` files describe **which widgets to use, in what order, with which labels and bindings**.

See also: [PDF-REPORT-SYSTEM.md §6](../.cursor/docs/PDF-REPORT-SYSTEM.md) (maintenance levels) and [§6.1 HPRP flow](../.cursor/docs/PDF-REPORT-SYSTEM.md).

## Package layout

Runtime composition prefers packed files in `packages/`, then falls back to unpacked JSON.

```
packages/                         # SoT for packed .hprp (scanned first)
  clinical-01-hct-epo.hprp
  clinical-03-hemodialysis-record.default.hprp
  clinical-03-hemodialysis-record.rama.hprp
  clinical-03-hemodialysis-record.thaiur.hprp

assets/templates/
  schema/                 # JSON schema
  _shared/                # copy-paste layout reference (not loaded)
  reports/                # unpacked source + fallback
    clinical-01-hct-epo/
      manifest.json
      layout.json
      labels.th.json
    clinical-03-hemodialysis-record/
      variants/
        default/
        rama/
        thaiur/
```

- `packages/{id}.hprp` → single-package report (ZIP of the JSON files)
- `packages/{id}.{variant}.hprp` → hospital layout of the same report
- `reports/{id}/` remains the editable source; HPRP Studio / `POST /api/hprp/pack-from-templates` regenerates `packages/`
- Scan of unpacked folders skips `schema`, `_shared`, `tenants`

Changing `layout.json` / labels in the unpacked folder does **not** require a C# rebuild, but **does not affect runtime** until you pack again (packed files win). Rebuild C# only when adding a widget id or a new `layoutKind`.

Open **HPRP Studio** (Visual Designer) at `http://localhost:5090/` or `http://localhost:5090/hprp-studio/` (Development). Use Bearer `dev` with mock auth. Writes require `HemoPdf:EnableHprpStudioWrite=true`.

### Experimental: `layoutMode: absolute`

Spike path (QuestPDF freeform mm) — **does not replace** composition packs.

| | Composition (default) | Absolute (experimental) |
|--|--|--|
| Manifest | omit `layoutMode` or `composition` | `layoutMode: "absolute"` |
| Layout | `body` / `sections` | `widgets[]` with `xMm` `yMm` `wMm` `hMm` |
| Studio | tree + Page canvas (reorder) | Absolute canvas drag/resize in mm |
| PDF | existing composers | `AbsoluteCanvasComposer` (QuestPDF Layers) |
| Sample | clinical-* packs | `experimental-absolute-demo`, `experimental-absolute-clinical-01` |

Composition clinical packs remain the production path. Absolute is for exploring a true page designer; delete the branch / ignore the demo package if the spike is abandoned.

**Dense widgets on absolute canvas (clinical-01 first):** use `type: "dense"` + `widget: "thaiur.header" | "clinical.hct-epo-annual-table" | "clinical.hct-epo-copay"`. Optional `chrome` / `columnPlan` match composition layout nodes. Annual table row height is budgeted from the placed `hMm` box so the same widget scales across layouts. Set `dataAdapter: "clinical-01-hct-epo"` (or place any clinical-01 dense widget) so preview binds real Hct/EPO sample data.

### Designer: `layoutMode: designer` (configurable table)

WYSIWYG path on branch `feat/hprp-table-designer` — **does not replace** composition packs until merge.

| | Composition (default) | Designer |
|--|--|--|
| Manifest | omit `layoutMode` or `composition` | `layoutMode: "designer"` |
| Layout | `body` / `sections` | `elements[]` with `box` mm + `type: config-table` |
| Studio | tree + Page canvas + Preview pane | **3-column**: palette · HTML canvas · inspector (no preview pane) |
| Table | fixed C# widget (`clinical.hct-epo-annual-table`) | preset `hct-epo-annual-v1` + bindings + column overrides |
| PDF | section composers | shared `HprpTableLayoutEngine` → `ConfigurableTableComposer` |
| Sample pack | `clinical-01-hct-epo` | `clinical-01-hct-epo` / `clinical-02-epo-drug` / `clinical-05-progress-note` (+ checklist) |

**Dense on designer canvas (clinical-05):** SOAP keeps `type: "dense"` + `widget: "clinical.soap-table"` (nested S/O/A/P pixels stay in C#). Checklist uses dense `clinical.checklist-patient` / `clinical.checklist-grid` / `clinical.checklist-text-notes` plus box-text title band. Row height for SOAP is budgeted from the placed `hMm` box (~2 session rows).

**Preset library:** `assets/templates/presets/tables/{id}.json` — reusable table chrome + columns + row mode (`freedom` | `monthly` | `annual`). Studio API: `GET/PUT /api/hprp/presets/tables/{id}`.

**Adapter schema (field mapper):** `assets/templates/adapters/{dataAdapterId}.schema.json` — `GET /api/hprp/adapters/{dataAdapterId}/schema` drives the Studio “Map field…” picker.

**config-table element:**

```json
{
  "id": "annual",
  "type": "config-table",
  "presetId": "hct-epo-annual-v1",
  "box": { "xMm": 0, "yMm": 29, "wMm": 206, "hMm": 228 },
  "bindings": [
    { "path": "months[].monthLabel", "column": "month", "context": "group-label" },
    { "path": "months[].entries[].hb", "column": "hb", "context": "entry" }
  ],
  "columnOverrides": []
}
```

Studio canvas renders HTML via `table-layout-engine.js` (same rules as C#). **Download PDF** calls `POST /api/hprp/preview` (QuestPDF verify only — no live preview iframe in designer mode).

See also: [HPRP-TABLE-DESIGNER-BRANCH.md](./HPRP-TABLE-DESIGNER-BRANCH.md) for branch-isolated breaking changes.

### Visual Designer (MVP)

Studio is still a **composition editor** (not freeform x/y). The **Page canvas** shows A4 flow cards for `layout.body` **and** `layout.sections` (hemosheet / SOAP / dense clinical). Dense widgets are opaque cards — reorder and edit chrome/labels only; inner pixels stay in C#.

| Surface | Role |
|---------|------|
| **Palette** | Human-readable titles + widget id; groups Clinical widgets / Hemosheet sections / Body blocks |
| **Structure tree** | Page → Labels → nodes; Up/Down/Remove; Place beside (body only) |
| **Page canvas** | Visual A4 sheet; click to select; drag to reorder siblings |
| **Inspector** | Page / Labels / node chrome; dense note when a C# widget is selected |
| **Preview / Download PDF** | Same `POST /api/hprp/preview` QuestPDF bytes (fidelity 100%) |
| **Import / Export .hprp** | Client ZIP (`manifest.json` + `layout.json` + `labels.*.json`); validate via API before apply/download |

Hemosheet **Place beside stays off** — sections remain a vertical stack.

| Studio button | Writes / action |
|---------------|-----------------|
| **Save and pack** | Editor draft → `packages/{id}.hprp` |
| **Pack this from disk** | `assets/templates/reports/{id}/` → that `.hprp`, then reloads the editor |
| **Pack all from disk** | every unpacked report folder → `packages/` |
| **Export .hprp** | Download validated draft ZIP (does not write server disk) |
| **Import .hprp** | Load ZIP into editor after validate; use Save and pack to persist |
| **Download PDF** | Same bytes as the Preview iframe |

#### Manual smoke checklist (Designer)

1. Open a **designer** package (e.g. `clinical-01-hct-epo`, `clinical-05-progress-note`) and a **composition** package (e.g. `clinical-03-hemodialysis-record` / `clinical-04-prescription`).
2. Confirm Page canvas lists the same nodes as the structure tree; reorder → Preview PDF updates.
3. **Download PDF** — file opens and matches the Preview iframe.
4. **Export .hprp** → **Import .hprp** the same file → draft restores; Validate OK.
5. Open **Labels** in Designer, change a string → Preview updates (no JSON mode).
6. Select a dense widget (SOAP / hemosheet section) — inspector shows the C# dense note; Place beside is absent on sections.

Pixels still live in C# (`HctEpoAnnualTableSection`, hemosheet section renderers, `ReportBlock` types). `.hprp` controls **which** widgets run, **order**, **labels**, extra form **blocks**, and catalog `ui` — not QuestPDF drawing code inside a dense widget.

### Labels vs layout (why a new key does not appear)

The **labels** tab is a dictionary of strings. Widgets and `$label` references **look up** keys they already know. Adding a new key does nothing until something in **layout** (or a C# widget) asks for that key.

| What you want | Where to edit | Notes |
|---------------|---------------|--------|
| Rename an existing header (`สปสช` → something else) | **labels** only | Widget already calls `HprpLabels.Get(..., "nhso", …)` |
| Add a new line/block on the page | **layout** `body` + optional **labels** | Use a form `type` (`text`, `key-value-table`, `field-grid`). No new C# widget. |
| Add a **column inside** `clinical.hct-epo-copay` / annual table | C# widget (+ DTO field if it is data) | Dense widgets still own their grid. Labels cannot invent columns. |

Example extra block (after pack, no compile). Duplicate JSON keys in one object are invalid — use one object per `rows[]` item:

```json
{
  "widget": "clinical.hct-epo-copay"
},
{
  "type": "key-value-table",
  "rows": [
    {
      "label": { "$label": "extraNote" },
      "content": "…"
    }
  ]
}
```

Then in labels: `"extraNote": "หมายเหตุเพิ่ม"`. Literal values belong in layout `content` (or `bind`), not only in the labels dictionary.

## manifest.json

| Field | Meaning |
|-------|---------|
| `id` | Canonical template id (`clinical-07-lab`, …) |
| `variant` | Folder key (`default` / `rama` / `thaiur`). Empty for single-package reports |
| `layoutKind` | Existing C# composer: `DefaultForm` \| `ThaiUrForm` \| `UniquePlanner` |
| `layoutProfile` | Tenant setting value (`Default` / `Rama` / `ThaiUr`) for hemosheet dropdown |
| `displayName` | Title used in metadata / report catalog |
| `engineVersion` | Must be `<=` engine current (1) |
| `dataAdapter` | Named C# fetch adapter (see below) |
| `requiresSignature` | Guard for generate |
| `language` | Default label language |
| `ui` | Optional FE menu / picker / parameter metadata (see below) |

### `ui` (FE catalog)

| Field | Meaning |
|-------|---------|
| `entryMode` | `hemosheetList` \| `patient` \| `patientMonth` \| `patientYear` \| `unitDateRound` |
| `menuGroup` | e.g. `clinical`, `standalone` |
| `sortOrder` | Menu sort key |
| `visibleInMenu` | When false, hidden from Reports accordion |
| `role` | `hemosheetLayoutProfile` — HemoAdmin layout dropdown |
| `profileLabel` | Dropdown label (catalog still uses `displayName`) |
| `reportDataPath` | Optional Web.Api path template for convention fetch |
| `parameters[]` | How FE builds preview `parameters` (`source`: `route` / `query` / `constant` / `default`) |

JSON schema: `assets/templates/schema/hprp-manifest.schema.json`

### dataAdapter values

| Id | Used by |
|----|---------|
| `flatten-dto` | Default form reports (04, 06, 07, 10–16) |
| `hemosheet-record` | clinical-03 |
| `clinical-01-hct-epo` | clinical-01 |
| `clinical-02-epo-drug` | clinical-02 |
| `clinical-05-progress-note` | clinical-05 |
| `consent` | clinical-08 / 09 |
| `medicine-preparation-round` | (reserved) |

Forbidden keys anywhere in layout JSON: `script`, `code`, `eval`, `csharp`, `javascript`, `lambda`.

## layout.json

### Form reports (`body`)

Nodes use `type` = existing `ReportBlock`:

| `type` | Purpose |
|--------|---------|
| `text` | Title / subtitle / paragraph (`style`: `title`, `subtitle`, `body`). Optional `chrome.fontSize` overrides the token. |
| `field-grid` | Label/value **grid** (`fields[]`, integer `columns`, per-field `columnSpan`) |
| `key-value-table` | Two-column rows; `appendFlatten: true` adds scalar DTO keys |
| `data-grid` | Tabular data via `bindRows` (JSONPath to array) |
| `patient-info` | Patient header block |
| `signature` | Signature slots from request context |
| `row` | Place child `cells[]` **on the same line**. Cell `width`: `*` / `40%` / `32mm`. Nested `nodes[]` stack inside a cell. |
| `column-stack` | Vertical stack of `nodes[]` (also implied when a cell has more than one node) |

Bind with JSONPath (`$.patient.hn`) or special binds: `$title`, `$subtitle`, `$flatten`.

`when` on a node: JSONPath expression (e.g. `"$.rows.length > 0"`) or omitted (= always show).

### `page`

Optional. **Omitted fields keep the composer C# defaults** (hemosheet 2mm, form `ReportPageLayout` 2/4mm). When set, Studio/PDF use the file.

| Field | Meaning |
|-------|---------|
| `size` | `A4` (default) |
| `marginMm` | Uniform margin (mm) for all sides |
| `margin` | `{ top, right, bottom, left }` — named sides override shorthand |
| `spacingMm` | Gap between stacked body blocks. On **clinical-05**, also the gap under the repeating `thaiur.header` before the SOAP table (0 = flush). |
| `fontSize` | Default body data font for primitive blocks |

Per-node `box.marginMm` / `box.paddingMm`: number, `[v,h]`, `[t,r,b,l]`, or named sides.

### Three meanings of “column”

| Where | What it changes | Studio control |
|-------|-----------------|----------------|
| `field-grid.columns` | How many label/value **cells per grid row** | Integer stepper + `columnSpan` |
| `columnPlan` / hemosheet `columns` | **Table data** columns (HCT/EPO, dialysis headers) | Add/remove/reorder in inspector |
| `row.cells[].width` | Side-by-side **blocks** | Place beside / cell width `*` `40%` `32mm` |

Dense widgets (SOAP, consent narrative, most hemosheet sections) still own inner pixels in C#. Chrome / recipe knobs only.

Studio is a **constraint / flow editor** with a visual **Page canvas** (`Page → Flow → Row/Cell → Block`), not a free absolute canvas. Do not promise extra SOAP/consent columns in the UI unless the recipe already has `columnPlan`.

### File knobs vs C# rebuild

| Driven by `.hprp` / Studio (pack, no compile) | Needs a C# widget / recipe change |
|-----------------------------------------------|-----------------------------------|
| Page `margin` / `spacingMm` / `fontSize` when the file sets them | Inner pixels of SOAP, HCT/EPO tables, consent narrative |
| Per-node `box`, `chrome.fontSize`, labels, bind paths | New widget ids, new `layoutKind` |
| `row` / `column-stack` around form blocks and allowed widgets | Hemosheet **section order** as a free grid; Place beside between hemosheet sections |
| `field-grid.columns`, `columnPlan` / dialysis `columns` when the recipe already lists them | Bind fields inside HCT tables outside the C# formula |

**Page margin:** omitted → composer C# default (hemosheet 2mm, forms 2/4mm). Set in the file → used. Forms that already wrote `marginMm: 10` or `8` start applying that value after pack.

**Hemosheet (`sections[]`):** Page inspector still applies. Place beside is **off** — vertical section order stays C#. Do not treat hemosheet as a free layout.

### `chrome` (table appearance)

Optional on form `body` nodes (`data-grid`, `field-grid`, `key-value-table`) and hemosheet `sections[]`. Omitted fields keep engine / tenant branding defaults.

| Field | Meaning |
|-------|---------|
| `headerFill` | `#RRGGBB` or `$branding.sectionHeaderBackground` |
| `border` | `none` / `thin` (default) / `medium` |
| `fontSize` | Data and column-header font size |
| `headerHeightMm` | Column-header bar height (SOAP / dense tables that read it) |
| `headerAlign` | `top` / `middle` (default) / `bottom` — label vertical align in the header bar |
| `headerPaddingMm` | Uniform inset inside the header cell |
| `rowHeightMm` | Min row height (`data-grid`) / body row height (SOAP) |
| `columnWidths` | Relative weights (`*` = 1). Applied only when count matches columns |
| `bandWeights` | SOAP S:O:A:P band weights (clinical-05) |

Examples: `clinical-07-lab` (lab DATE matrix) and `clinical-06-medication` (Med History matrix: Medication / Frequency / Physician + 5 live dates with ✓/X — not a prescription list). Pack after editing — runtime reads `packages/*.hprp` first.

### Hemosheet (`sections`)

Uses `sections[]` with C# widget ids. `when` tokens:

| Prefix | Example | Meaning |
|--------|---------|---------|
| `feature:` | `feature:showAvPanel` | `LayoutContext.Features` |
| `profile:` | `profile:ThaiUr` | Hemosheet layout profile |
| `data:` | `data:hasLabData` | Derived from view model |
| `not-` | `not-profile:ThaiUr` | Negation |
| `or:` | `or:a,b` | Any match |

Optional per section: `variant`, `columns`, `columnsWhen`, `fixedLinesFrom`, `chrome`.

Dense Default / Thai UR dialysis tables **read `columns` / `columnsWhen` from this file** when the count matches the C# grid (data columns + Note). Rama still uses `columns` as named mapper keys (`เวลา`, `HR`, …).

JSON schema: `assets/templates/schema/hprp-layout.schema.json`

### Dedicated reports (01, 02, 05, 08, 09)

`layout.json` declares widget ids **and optional form `type` blocks**. Dedicated composers resolve order via shared **`HprpLayoutPlan.ResolveNodes`** + **`HprpWidgetDispatch`** (handler map per report). Extra `text` / `key-value-table` / `field-grid` / `data-grid` nodes render through `HprpBinder.BindGeneric` + `ReportBlockPdfComposer` — no new widget id.

| Report | `.hprp` / data drives today |
|--------|------------------------------|
| **clinical-03** | `layout.sections` → planner (Rama) or dense form (Default/Thai UR). Dense dialysis **headers** come from `columns` / `columnsWhen` + optional `chrome` |
| **clinical-01** | Section order (`header`+`body`) + labels + extra form blocks — pixels of dense widgets stay in C# sections |
| **clinical-02** | Same; `clinical.epo-drug-table` includes meta band (not a separate widget yet) |
| **clinical-05** | `layout.header` → repeating page header; `layout.body` → SOAP + extra form blocks |
| **clinical-08/09** | `layout.header` + `clinical.consent-narrative` body + extra form blocks; narrative internals stay C# |
| **clinical-04** | ThaiUr header + doctor-prescription style body (`$.dialysisFields` / med lines) via `Clinical04PrescriptionReportDataService` |
| **clinical-06 / 10–16** | Trusted `report-data` via `ClinicalFormReportDataService` + HPRP `$.fields` / `$.rows` |
| **clinical-07** | Dedicated lab matrix endpoint (unchanged) |

Tenant override can reorder widgets that the allow-list understands (e.g. clinical-01 co-pay above annual table) and insert form blocks without rebuilding the engine.

### Widget reuse (efficiency model)

Three layers — do **not** invent a new plan class per report:

| Layer | What | Reuse |
|-------|------|--------|
| **1. Section drawers** | `ThaiUrReportHeader`, `HctEpoCoPayCriteriaSection`, … | Shared C#; one implementation |
| **2. Widget handlers** | `Dictionary<widgetId, Action<IContainer>>` in each composer | Thin VM→section glue; share drawers across reports (01+02 co-pay) |
| **3. Plan + dispatch** | `HprpLayoutPlan` + `HprpWidgetDispatch` | One loop; allow-list per report |

**Do**

- Add a widget id only when it is a reusable visual primitive (or a clear composition seam).
- Register the same section drawer under different reports’ handlers when the pixels match.
- Keep allow-lists tight so foreign widgets never draw on the wrong form.

**Avoid**

- One mega registry of all reports’ DTO types (erases type safety, hard to DI).
- Per-report `HprpClinical0xLayoutPlan` clones.
- Splitting every internal band into widgets before order actually needs to change (e.g. epo meta stays inside `clinical.epo-drug-table` until a tenant must reorder it).

**Later (max reuse):** map dense widgets → real `ReportBlock`s + `ReportBlockPdfComposer` so Default path can drop dedicated composers entirely — only after block primitives cover pixel parity.

## Widget catalog

Source of truth: `src/Hemo.Pdf.Core/Hprp/HprpWidgetIds.cs`

### ReportBlock types (form `body`)

`field-grid`, `key-value-table`, `data-grid`, `text`, `signature`, `patient-info`

### Clinical widgets (dedicated composers)

| Widget id | Report |
|-----------|--------|
| `thaiur.header` | Shared ThaiUR header chrome |
| `clinical.hct-epo-annual-table` | clinical-01 |
| `clinical.hct-epo-copay` | clinical-01 |
| `clinical.epo-drug-table` | clinical-02 |
| `clinical.soap-table` | clinical-05 |
| `clinical.consent-narrative` | clinical-08 / 09 |

### Hemosheet section widgets (clinical-03)

`hemosheet.sub-header-bar`, `hemosheet.patient`, `hemosheet.session-meta`, `hemosheet.predialysis`, `hemosheet.vascular-access`, `hemosheet.assessment-pre-re`, `hemosheet.assessment-re`, `hemosheet.assessment-post`, `hemosheet.nursing-care-plan`, `hemosheet.assessment-other`, `hemosheet.labs`, `hemosheet.dialysis-records`, `hemosheet.uf-summary`, `hemosheet.nurse-records`, `hemosheet.doctor-records`, `hemosheet.medicine-records`, `hemosheet.progress-notes`, `hemosheet.footer-checklists`, `hemosheet.pre-post-hd-notes`, `hemosheet.post-vitals`, `hemosheet.avf-assessment`, `hemosheet.consent`

## Hybrid load

1. `FileHprpTemplateStore` scans `packages/*.hprp` **first** (mtime reload — no compile)
2. Then loads unpacked `assets/templates/reports/` only for ids/variants not already packed
3. Lookup key is `{id}#{variant}` (`variant` from tenant `LayoutProfile`: Default→`default`, Rama→`rama`, ThaiUr→`thaiur`)
4. Invalid / newer engine version → skip that file/folder (never HTTP 500)
5. After Studio pack, `Invalidate()` forces the next request to rescan

Tenant ZIP overlays under `assets/templates/tenants/` are **not** the production path. Add a new hospital layout by committing `reports/{id}/variants/{key}/`, packing to `packages/`, and deploying Hemo-PDF.

Resolve order: packed `.hprp` → unpacked folder → `HprpCatalog.TryGetDefinition` → `ClinicalReportCatalog` fallback.

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/report-catalog?menuOnly=` | FE menu catalog (manifest `ui` + fetch/renderer capability) |
| `GET` | `/api/templates` | Unique report manifests (default variant) |
| `GET` | `/api/templates?role=hemosheetLayoutProfile` | Hemosheet layout dropdown (`variant`, `layoutKind`, `layoutProfile`, `profileLabel`) |
| `GET` | `/api/templates/{id}?variant=` | Manifest for that variant |
| `POST` / `DELETE` | `/api/templates/{id}` | **410 Gone** — tenant uploads disabled |
| `GET` | `/api/hprp/catalog` | Widget / adapter / entryMode lists for Studio |
| `GET` | `/api/hprp/packages` | Cached packages (packed + folder fallback) |
| `GET` | `/api/hprp/packages/{id}?variant=` | Full manifest + layout + labels |
| `PUT` | `/api/hprp/packages/{id}` | Validate and write `packages/{id}[.variant].hprp` (Studio write flag) |
| `POST` | `/api/hprp/validate` | Dry-run `HprpValidator` |
| `POST` | `/api/hprp/pack-from-templates` | Pack every unpacked report into `packages/` |
| `POST` | `/api/hprp/pack-from-templates/{id}` | Pack one report (all variants) |

Requires `Authorization: Bearer` + `X-Tenant-Code` (mock dev: any bearer + header). Studio UI: `GET /` or `GET /hprp-studio/`.

Config:

- `HemoPdf:TemplatesRootPath` (default `assets/templates`)
- `HemoPdf:PackagesRootPath` (default `packages`)
- `HemoPdf:EnableHprpStudioWrite` (Development `true`; production `false`)

### Adding a new report (after dynamic catalog)

1. **Web.Api:** dedicated `GET api/Patients/{id}/reports/{new-id}/report-data` (if real data is needed)
2. **Hemo-PDF:** `assets/templates/reports/{new-id}/` (manifest with `ui`, layout, labels), then pack via Studio or `POST /api/hprp/pack-from-templates/{id}` into `packages/`. New hemosheet hospital: `reports/clinical-03-hemodialysis-record/variants/{key}/` with an existing `layoutKind`
3. **Frontend:** no code change — menu appears from `GET /api/report-catalog`; HemoAdmin dropdown reads `GET /api/templates?role=hemosheetLayoutProfile`

## Runtime flow (short)

```
GeneratePdfRequest
  → FileHprpTemplateStore (packages/*.hprp first, then reports/{id})
  → ReportPipeline (metadata, signature guard from manifest)
  → Renderer factory
      Form (04,06,07,10-16): ClinicalDefaultDataProvider → HprpBinder → ReportBlock[]
      Hemosheet (03):         layoutKind from manifest → DefaultForm / ThaiUrForm / UniquePlanner
                              UniquePlanner: HemosheetLayoutPlanner → HprpHemosheetPlanInterpreter
      Dedicated (01,02,05,08): C# composer + HprpLayoutPlan / HprpLabelResolver from packed file
  → QuestPDF / ReportDocument JSON
```

## What still needs a rebuild

New block/widget type, new `layoutKind` composer, new Web.Api DTO/adapter, engine bugs, new Angular preview primitive.

## HemoAdmin layout profile

HemoAdmin loads dropdown options from `GET /api/tenants/{id}/hprp-templates?role=hemosheetLayoutProfile` (proxy to Hemo-PDF). Tenant save still writes `LayoutProfile` and syncs the dual-stack `.trdp` name. There is no upload UI.
