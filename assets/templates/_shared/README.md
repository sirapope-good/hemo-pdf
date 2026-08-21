# Shared HPRP layout reference

These JSON files are **not loaded at runtime**. `FileHprpTemplateStore` skips the `_shared` folder.

Copy into `assets/templates/reports/{template-id}/layout.json` when scaffolding a new form report. Prefer `clinical-07-lab/layout.json` when the report needs a `data-grid`.

Signed forms (e.g. clinical-04) add a `signature` block — see `clinical-form-layout-signed.json`.
