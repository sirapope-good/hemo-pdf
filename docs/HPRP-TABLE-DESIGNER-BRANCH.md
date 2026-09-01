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

## Studio chrome (toolbar + loading)

Canvas toolbar is grouped (not a flat button strip):

| Group | Controls |
|--|--|
| **View** | Undo / Redo / Zoom − % + / Fit |
| **Insert** (dropdown) | Header, Table, **Data grid**, Box text, Fragment, Page of |
| **Library** | Add to library |
| **Export** | PDF (Download PDF) |
| Sample | Mock scenario select (standalone) |

Left pane: **Import / Export** primary; **Pack all** under Packages ⋯; Library **Edit / Insert** primary; **Save from selection / Delete** under ⋯.

Loading feedback: brand overlay spinner (`StudioUi.withBusy`) on open package / library item / save / reload / pack-all; list skeletons on package/library reload; A4 canvas skeleton while a pack opens.

Scripts use `?v=ux-polish-15` cache-bust — hard refresh (Ctrl+F5) after pull.

Top-left brand: inline HPRP wordmark logo (from `assets/icons/LOGO_HPRP.svg`, `currentColor` for dark chrome) — replaces the old title + hint paragraph.

## Column stack (inner section)

Fill empty space under a shorter beside sibling without starting a full-width outer row:

| Concept | Detail |
|--|--|
| JSON | `type: "group"`, `direction: "column"`, `children[]` (max **4**) |
| Insert | **Insert → Table · inner below** / **Box text · inner below** (selection required; wraps leaf into group if needed) |
| Chrome | Dashed group frame on canvas |
| Splitter | Drag the accent line between stacked children to redistribute height (above + below) |
| PDF | `HprpDesignerFlow` packs groups; paint expands to leaf children (parity with Studio) |

Outer **Insert → Table / Box text** still appends a new outer row (previous behavior).


## Canvas tools

- **Undo / Redo** — toolbar + `Ctrl+Z` / `Ctrl+Y`
- **Pan** — hold **Space** + drag
- **Zoom** — mouse wheel (or ± / Fit)
- **Multi-select** — **Shift+click** on canvas elements

## Multi-page + bands

When **content** exceeds the content band height, Studio shows page 2+ automatically (orange outline) and PDF emits matching pages. Assign each block a **band** in the inspector:

| Band | Behavior |
|--|--|
| `super-header` | Top chrome (e.g. report name), repeats |
| `header` | Header chrome, repeats (default for `type: header`) |
| `content` | Flows; may create extra pages |
| `footer` / `super-footer` | Bottom chrome, repeats (e.g. page of) |

**Super bands** always render **outside** the dashed margin guide (in the margin gutter). Inner header/content/footer stay inside.

### Optional content (`omitWhenEmpty`)

Content blocks can declare `"omitWhenEmpty": "$.textNotes"`. When that JSON path is empty (empty array / blank / missing), the block is **skipped in reflow** for both Studio and PDF. Use this for reserved sections (e.g. checklist text notes) so an empty notes slot does not push a second page that only repeats the header. Trailing content pages with zero blocks are also trimmed. Packs without the property still omit `clinical.checklist-text-notes` when `textNotes` is empty (compat fallback).

### Page of

Element type `page-of` (default band `super-footer`): format `{current} / {total}` — Studio button **+ Page of**. PDF uses QuestPDF page numbers.

## Assets

- Table presets: `assets/templates/presets/tables/hct-epo-annual-v1.json`, `copay-nhso-v1.json`, `copay-sso-v1.json`, `epo-drug-injections-v1.json`, **`progress-note-soap-v1.json`**, **`progress-note-checklist-matrix-v1.json`** (`rowMode: matrix`)
- Fragments: `progress-note-checklist-patient-v1`, `progress-note-checklist-matrix-v1`, `progress-note-checklist-notes-v1`, `progress-note-checklist-body-v1` (Library → Fragments — insertable pieces)
- Header preset: `assets/templates/presets/headers/clinical-header-thaiur.json`
- Adapter schema: `assets/templates/adapters/clinical-01-hct-epo.schema.json`, `clinical-02-epo-drug.schema.json`, `clinical-05-progress-note.schema.json`, `clinical-05-progress-note-checklist.schema.json`
- Production clinical-01: `layoutMode: designer` (`assets/templates/reports/clinical-01-hct-epo/`)
- Production clinical-02: `layoutMode: designer` — multi-item **box-text** meta + freedom injections table + shared co-pay duo
- Production clinical-05 SOAP: `layoutMode: designer` — `clinical-header-thaiur` + **`config-table`** preset `progress-note-soap-v1` (`rowMode: freedom`, progress column `cellKind: soap-progress`) + page-of. Same library-table tools as HCT/EPO: column drag, freedom rows, detach/save preset; drag S/O/A/P band splitters (or edit `chrome.bandWeights`) for Objective height. PDF still draws Objective checkboxes via `Clinical05SoapTableSection.ComposeProgressCell`. Opening an old pack with dense `clinical.soap-table` auto-migrates to this config-table.
- Production clinical-05 checklist (Default): `layoutMode: designer` — **same** `clinical-header-thaiur` with element `bottomMode: "checklist-patient"` (DOB / sessions / days / mode / underlying in a 2-line bottom; diagnosis profile stays default for SOAP/other packs) + range + **`config-table`** matrix + dense text-notes. Dense patient block is removed (auto-migrated). Header profiles live in `bottomFieldSets` on the shared ThaiUR library preset.
- Production **clinical-07-lab**: `layoutMode: designer` — `clinical-header-thaiur` + designer element **`data-grid`** (`bindRows: $.rows`, `columnHeadersBind: $.columnHeaders`, `chrome.columnWidths` per column). PDF via `DesignerPageComposer.DrawDataGrid` + `HprpDataGridColumnPlan` (lab token + date columns expand when header count changes). Studio: inspector column tokens, canvas drag, **Insert → Data grid**. ไม่ใช้ config-table `rowMode: lab` — คอลัมน์ DATE dynamic จาก DTO.
- **Matrix column widths (2 zones):** `chrome.columnWidths: ["item", "monthBand"]` — each token is fixed (`46mm`) or relative (`*` / `1.5`). Month-band splits equally across N months. Studio: inspector fields + drag Item|Months edge; Library matrix opens with checklist sample. Shared resolver: `HprpMatrixColumnPlan`.

## box-text multi-value

`items[]` on `box-text` (takes precedence over single `text`/`bind`):

| Field | Meaning |
|--|--|
| `label` / `bind` / `text` | Primary labeled value |
| `label2` / `bind2` / `text2` | Optional second pair (e.g. พ.ศ. + year) |
| `align` | `left` / `center` / `right` per item |
| `flex` | Relative row weight (default 1) |

Used by clinical-02 meta band (เดือน | ยา EPO | เข็ม/สัปดาห์). Co-pay banner stays single-value.

## Data grid (lab matrix)

Designer element `type: "data-grid"` — bound rows + column headers (not config-table `rowMode: lab`).

| Field | Detail |
|--|--|
| `bindRows` | JSON path to row matrix (e.g. `$.rows`) |
| `columnHeadersBind` | Path to header labels (e.g. `$.columnHeaders`) |
| `chrome.columnWidths` | Relative tokens per column (`3`, `*`, `2`, `12mm`) |

**Column plan (`HprpDataGridColumnPlan`):** when token count matches header count, exact parse; otherwise token[0] = lab column, token[1] (or `*`) repeats for each DATE column. Studio mirrors the same rules for preview, inspector inputs, and canvas drag.

| Studio | Detail |
|--|--|
| Inspector | Per-column token inputs + **Sync to column count** (normalize tokens) |
| Canvas | Drag vertical splitters between header cells to resize columns |
| Insert | **Insert → Data grid** adds a default lab matrix block |
| Section bands | Rows with label only in column 1 (`1 Month`, `3 Month`, …) merge all columns + light header fill (PDF + Studio) |

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

**Not in this pass:** per-tenant folders beyond `packages/library/`; nested groups inside groups (one column-stack level only).
