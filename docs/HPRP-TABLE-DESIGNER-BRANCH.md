# HPRP Table Designer branch notes

Branch: `feat/hprp-table-designer` (isolated from `main` until merge).

## Studio UX (this branch)

**Canvas is the editor** — not a tree + separate PDF preview.

| Removed / hidden | Replacement |
|--|--|
| Structure tree / Body order | Click elements on the A4 HTML canvas |
| Schematic page cards | Real HTML table from shared layout engine |
| Preview pane (iframe PDF) | **Download PDF** only (QuestPDF verify) |
| Dense widget palette as primary | `config-table` + preset `hct-epo-annual-v1` |

Layout: **Packages | Page canvas (WYSIWYG) | Inspector**

Opening `clinical-01-hct-epo` loads `layoutMode: designer` with config-table. Column +/−, row mode, slots, and field mapping update the canvas immediately.

## Breaking vs production packs

| Ofเดิม | On this branch |
|--|--|
| Composition `layout.body[]` Studio tree | Studio does not use it; clinical-01 source is designer |
| Dense `clinical.hct-epo-annual-table` in clinical-01 | Replaced by `config-table` + preset |
| Absolute dense clone | Superseded for Studio editing |
| `experimental-absolute-*` | Optional demos; designer path is preferred |

Packed `.hprp` on `main` is unchanged until this branch merges.

## Assets

- Preset: `assets/templates/presets/tables/hct-epo-annual-v1.json`
- Adapter schema: `assets/templates/adapters/clinical-01-hct-epo.schema.json`
- Sample designer pack (alias): `assets/templates/reports/clinical-01-hct-epo-designer/`
- Production clinical-01 on this branch: `layoutMode: designer` + same elements
