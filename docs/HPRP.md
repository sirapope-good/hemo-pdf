# Hemopro Report Package (`.hprp`)

Composition documents for Hemo-PDF. The engine (QuestPDF widgets, `ReportBlock` types, data adapters) stays in C#. `.hprp` files describe **which widgets to use, in what order, with which labels and bindings**.

See also: [PDF-REPORT-SYSTEM.md §6](../.cursor/docs/PDF-REPORT-SYSTEM.md) (maintenance levels) and [§6.1 HPRP flow](../.cursor/docs/PDF-REPORT-SYSTEM.md).

## Package layout

A `.hprp` file is a ZIP. Repo defaults are stored unpacked under `assets/templates/{id}/`:

- `manifest.json`
- `layout.json`
- `labels.th.json` / `labels.en.json` (optional)
- `assets/` (optional static images)

## manifest.json

| Field | Meaning |
|-------|---------|
| `id` | Canonical template id (`clinical-07-lab`, …) |
| `displayName` | Title used in metadata |
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

`hemosheet.sub-header-bar`, `hemosheet.session-meta`, `hemosheet.predialysis`, `hemosheet.vascular-access`, `hemosheet.assessment-pre-re`, `hemosheet.assessment-re`, `hemosheet.assessment-post`, `hemosheet.nursing-care-plan`, `hemosheet.assessment-other`, `hemosheet.labs`, `hemosheet.dialysis-records`, `hemosheet.uf-summary`, `hemosheet.nurse-records`, `hemosheet.doctor-records`, `hemosheet.medicine-records`, `hemosheet.progress-notes`, `hemosheet.footer-checklists`, `hemosheet.pre-post-hd-notes`, `hemosheet.post-vitals`, `hemosheet.avf-assessment`, `hemosheet.consent`

## Hybrid load

1. Default from `assets/templates/{id}/` (folder or `.hprp` zip at root)
2. Tenant override ZIP at `assets/templates/tenants/{tenant}/{id}.hprp` (upload `POST /api/templates/{id}`)
3. Invalid / newer engine version → fall back to default (never HTTP 500)

Resolve order: `FileHprpTemplateStore.TryGetCached` → `HprpCatalog.TryGetDefinition` → `ClinicalReportCatalog` fallback.

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/report-catalog?menuOnly=` | FE menu catalog (manifest `ui` + fetch/renderer capability) |
| `GET` | `/api/templates` | List default manifests + `hasTenantOverride` |
| `GET` | `/api/templates/{id}` | Manifest + override flag |
| `POST` | `/api/templates/{id}` | Upload tenant `.hprp` (zip or multipart) |
| `DELETE` | `/api/templates/{id}` | Remove tenant override |

Requires `Authorization: Bearer` + `X-Tenant-Code` (mock dev: any bearer + header).

Config: `HemoPdf:TemplatesRootPath` (default `assets/templates`).

### Adding a new report (after dynamic catalog)

1. **Web.Api:** dedicated `GET api/Patients/{id}/reports/{new-id}/report-data` (if real data is needed)
2. **Hemo-PDF:** `assets/templates/{new-id}/` (manifest with `ui`, layout, labels) **or** upload `.hprp` via HemoAdmin; register dedicated fetch only when not using `reportDataPath` convention
3. **Frontend:** no code change — menu appears from `GET /api/report-catalog`

## Runtime flow (short)

```
GeneratePdfRequest
  → FileHprpTemplateStore (tenant override → default folder)
  → ReportPipeline (metadata, signature guard from manifest)
  → Renderer factory
      Form (04,06,07,10-16): ClinicalDefaultDataProvider → HprpBinder → ReportBlock[]
      Hemosheet (03):         HemosheetLayoutPlanner → HprpHemosheetPlanInterpreter → section renderers
      Dedicated (01,02,05,08): C# composer + HprpLabelResolver
  → QuestPDF / ReportDocument JSON
```

## What still needs a rebuild

New block/widget type, new Web.Api DTO/adapter, engine bugs, new Angular preview primitive.

## Upload from HemoAdmin

When `pdfApiUrl` is set on the tenant, HemoAdmin proxies upload via `POST /api/tenants/{id}/hprp-templates/{templateId}` → Hemo-PDF `POST /api/templates/{templateId}`.
