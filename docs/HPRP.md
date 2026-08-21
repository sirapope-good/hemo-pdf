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

Open **HPRP Studio** at `http://localhost:5090/` or `http://localhost:5090/hprp-studio/` (Development). Use Bearer `dev` with mock auth. The UI edits JSON, validates with `HprpValidator`, and writes `packages/*.hprp`. Writes require `HemoPdf:EnableHprpStudioWrite=true`.

Pixels still live in C# (`HctEpoAnnualTableSection`, hemosheet section renderers, `ReportBlock` types). `.hprp` controls **which** widgets run, **order**, **labels**, and catalog `ui` — not QuestPDF drawing code.

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
| `text` | Title / subtitle / paragraph (`style`: `title`, `subtitle`, `body`) |
| `field-grid` | Label/value columns (`fields[]`, `columns`) |
| `key-value-table` | Two-column rows; `appendFlatten: true` adds scalar DTO keys |
| `data-grid` | Tabular data via `bindRows` (JSONPath to array) |
| `patient-info` | Patient header block |
| `signature` | Signature slots from request context |

Bind with JSONPath (`$.patient.hn`) or special binds: `$title`, `$subtitle`, `$flatten`.

`when` on a node: JSONPath expression (e.g. `"$.rows.length > 0"`) or omitted (= always show).

### Hemosheet (`sections`)

Uses `sections[]` with C# widget ids. `when` tokens:

| Prefix | Example | Meaning |
|--------|---------|---------|
| `feature:` | `feature:showAvPanel` | `LayoutContext.Features` |
| `profile:` | `profile:ThaiUr` | Hemosheet layout profile |
| `data:` | `data:hasLabData` | Derived from view model |
| `not-` | `not-profile:ThaiUr` | Negation |
| `or:` | `or:a,b` | Any match |

Optional per section: `variant`, `columns`, `columnsWhen`, `fixedLinesFrom`.

JSON schema: `assets/templates/schema/hprp-layout.schema.json`

### Dedicated reports (01, 02, 05, 08, 09)

`layout.json` declares widget ids. Dedicated composers resolve order via shared **`HprpLayoutPlan`** + **`HprpWidgetDispatch`** (handler map per report).

| Report | `.hprp` / data drives today |
|--------|------------------------------|
| **clinical-03** | `layout.sections` → planner (`HprpHemosheetPlanInterpreter`); profile จาก BE `LayoutProfile` (ไม่พึ่งชื่อ `.trdp`) |
| **clinical-01** | Section order (`header`+`body`) + labels — pixels in C# sections |
| **clinical-02** | Same; `clinical.epo-drug-table` includes meta band (not separate widget yet) |
| **clinical-05** | `layout.header` → repeating page header; `layout.body` → SOAP |
| **clinical-08/09** | `layout.header` + `clinical.consent-narrative` body; narrative internals stay C# |
| **clinical-04 / 06 / 10–16** | Trusted `report-data` via `ClinicalFormReportDataService` + HPRP `$.fields` / `$.rows` |
| **clinical-07** | Dedicated lab matrix endpoint (unchanged) |

Tenant override can reorder widgets that the allow-list understands (e.g. clinical-01 co-pay above annual table) without rebuilding the engine.

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
