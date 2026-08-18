# Hemopro Report Package (`.hprp`)

Composition documents for Hemo-PDF. The engine (QuestPDF widgets, `ReportBlock` types, data adapters) stays in C#. `.hprp` files describe **which widgets to use, in what order, with which labels and bindings**.

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
| `dataAdapter` | Named C# fetch adapter |
| `requiresSignature` | Guard for generate |
| `language` | Default label language |

Forbidden keys anywhere in layout JSON: `script`, `code`, `eval`, `csharp`, `javascript`, `lambda`.

## layout.json

**Form reports** use `body` nodes with `type` = existing `ReportBlock` (`field-grid`, `key-value-table`, `data-grid`, `text`, `signature`, `patient-info`). Bind with JSONPath (`$.patient.hn`) or `$title` / `$flatten`.

**Hemosheet** uses `sections` that name C# widgets (`hemosheet.dialysis-records`). `when` tokens: `feature:*`, `profile:*`, `data:*`, `not-*`, `or:a,b`.

## Hybrid load

1. Default from `assets/templates/{id}/`
2. Tenant override ZIP at `assets/templates/tenants/{tenant}/{id}.hprp` (upload `POST /api/templates/{id}`)
3. Invalid / newer engine version → fall back to default (never HTTP 500)

## What still needs a rebuild

New block/widget type, new Web.Api DTO/adapter, engine bugs, new Angular preview primitive.
