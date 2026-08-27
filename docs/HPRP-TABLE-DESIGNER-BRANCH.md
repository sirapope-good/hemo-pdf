# HPRP Table Designer branch (`feat/hprp-table-designer`)

Experimental branch — **does not affect `main` until merged.**

## Breaking changes on this branch only

| Removed / deprecated | Replacement |
|---------------------|-------------|
| Separate Preview panel in Studio | HTML WYSIWYG `#designerCanvas` |
| Composition schematic Page canvas (designer mode) | Canvas renders config-table via client engine |
| Absolute dense widget clone for clinical-01 | `layoutMode: designer` + `config-table` + presets |
| `experimental-absolute-clinical-01` (superseded) | `clinical-01-hct-epo-designer` |

## Runtime compatibility

- **`layoutMode` omitted / `composition`:** unchanged — existing `.hprp` and C# dense composers still work.
- **`layoutMode: designer`:** new `DesignerPageComposer` + `ConfigurableTableComposer`.
- **`layoutMode: absolute`:** still supported (legacy spike); Studio defaults to designer when manifest says so.

## Assets

- Table presets: `assets/templates/presets/tables/*.json`
- Adapter field trees: `assets/templates/adapters/*.schema.json`
- Sample designer pack: `assets/templates/reports/clinical-01-hct-epo-designer/`
