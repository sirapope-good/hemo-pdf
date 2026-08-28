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
- config-header (`clinical-header-thaiur`)
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

- Table presets: `assets/templates/presets/tables/hct-epo-annual-v1.json`, `copay-nhso-v1.json`, `copay-sso-v1.json`, `epo-drug-injections-v1.json`
- Header preset: `assets/templates/presets/headers/clinical-header-thaiur.json`
- Adapter schema: `assets/templates/adapters/clinical-01-hct-epo.schema.json`, `clinical-02-epo-drug.schema.json`
- Sample designer pack (alias): `assets/templates/reports/clinical-01-hct-epo-designer/`
- Production clinical-01 on this branch: `layoutMode: designer` + same elements
- Production clinical-02: `layoutMode: designer` — multi-item **box-text** meta + freedom injections table + shared co-pay duo

## box-text multi-value

`items[]` on `box-text` (takes precedence over single `text`/`bind`):

| Field | Meaning |
|--|--|
| `label` / `bind` / `text` | Primary labeled value |
| `label2` / `bind2` / `text2` | Optional second pair (e.g. พ.ศ. + year) |
| `align` | `left` / `center` / `right` per item |
| `flex` | Relative row weight (default 1) |

Used by clinical-02 meta band (เดือน | ยา EPO | เข็ม/สัปดาห์). Co-pay banner stays single-value.

## Library tab + recovery

Left pane: **Packages | Library** (Headers / Tables / Fragments).

| Action | Effect |
|--|--|
| **Click** Headers / Tables / Fragments | Opens that preset alone on the canvas (title `Library · …`) |
| **Save** (main toolbar) while editing a library item | Writes `packages/library/{headers\|tables\|fragments}/{id}.json` — **not** a report `.hprp` |
| **Delete** (Library) | Removes library JSON only — seed under `assets/` is never deleted; if seed exists the id reappears from seed |
| Delete element on pack canvas + Save pack | Removes element from `layout.elements` only — disk preset stays |
| **Add to library** (canvas toolbar) | Saves selection → Header / Table / Fragment under `packages/library/…` |
| **Shift+click** on canvas | Multi-select elements; Add to library with 2+ items → Fragment (layout order) |
| Save from selection | `PUT /api/hprp/presets/{headers\|tables\|fragments}/{id}` (same library folder) |

### Library storage

| Kind | Seed | Studio override |
|--|--|--|
| Headers | `assets/templates/presets/headers/` | `packages/library/headers/` |
| Tables | `assets/templates/presets/tables/` | `packages/library/tables/` |
| Fragments | `assets/templates/presets/fragments/` | `packages/library/fragments/` |

Naming convention for clinical headers: `clinical-header-{tenant}` — e.g. `clinical-header-thaiur`, later `clinical-header-default`, `clinical-header-rama`. Legacy id `thaiur-header-v1` aliases to `clinical-header-thaiur`.

Optional `tags` on header/table/fragment presets for filtering (e.g. `tenant:hogwarts`).

**Not in this pass:** per-tenant folders beyond `packages/library/`, `type: group` wrapper.
