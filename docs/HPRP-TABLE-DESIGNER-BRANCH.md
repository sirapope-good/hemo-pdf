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

Opening `clinical-01-hct-epo` loads `layoutMode: designer` with:
- config-header (`thaiur-header-v1`)
- config-table annual (`hct-epo-annual-v1`)
- **box-text** co-pay banner + **two freedom tables** (`copay-nhso-v1`, `copay-sso-v1`, beside) — replaces dense `clinical.hct-epo-copay`

## Block spacing (Page inspector)

| Mode | Behavior |
|--|--|
| `custom` (default) | `spacingMm` both ways; optional `spacingBelowMm` / `spacingBesideMm` |
| `margin` | Gap = page `marginMm` |
| `none` | Gap 0 + slight overlap so borders look like one line |

Studio reflow and PDF (`HprpDesignerFlow.Reflow`) share the same rules.

## Canvas tools

- **Undo / Redo** — toolbar + `Ctrl+Z` / `Ctrl+Y`
- **Pan** — hold **Space** + drag
- **Zoom** — mouse wheel (or ± / Fit)

## Multi-page + bands

When **content** exceeds the content band height, Studio shows page 2+ automatically (orange outline) and PDF emits matching pages. Assign each block a **band** in the inspector:

| Band | Behavior |
|--|--|
| `super-header` | Top chrome (e.g. report name), repeats |
| `header` | Header chrome, repeats (default for `type: header`) |
| `content` | Flows; may create extra pages |
| `footer` / `super-footer` | Bottom chrome, repeats (e.g. page of) |

**Super bands** always render **outside** the dashed margin guide (in the margin gutter). Inner header/content/footer stay inside.

### Page of

Element type `page-of` (default band `super-footer`): format `{current} / {total}` — Studio button **+ Page of**. PDF uses QuestPDF page numbers.

## Assets

- Table presets: `assets/templates/presets/tables/hct-epo-annual-v1.json`, `copay-nhso-v1.json`, `copay-sso-v1.json`
- Header preset: `assets/templates/presets/headers/thaiur-header-v1.json`
- Adapter schema: `assets/templates/adapters/clinical-01-hct-epo.schema.json`
- Sample designer pack (alias): `assets/templates/reports/clinical-01-hct-epo-designer/`
- Production clinical-01 on this branch: `layoutMode: designer` + same elements
