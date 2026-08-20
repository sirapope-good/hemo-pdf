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

`layout.json` declares widget ids (`clinical.hct-epo-annual-table`, …) for documentation and future composition. **Today** the C# composer still renders pixel layout; `.hprp` supplies **labels** via `HprpLabelResolver`.

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
| `GET` | `/api/templates` | List default manifests + `hasTenantOverride` |
| `GET` | `/api/templates/{id}` | Manifest + override flag |
| `POST` | `/api/templates/{id}` | Upload tenant `.hprp` (zip or multipart) |

Requires `Authorization: Bearer` + `X-Tenant-Code` (mock dev: any bearer + header).

Config: `HemoPdf:TemplatesRootPath` (default `assets/templates`).

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
