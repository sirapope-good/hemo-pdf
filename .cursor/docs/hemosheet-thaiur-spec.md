# Hemosheet ThaiUR - Pixel Layout Spec

Source of truth: `Hemo-Report/Hemosheet-ThaiUR.trdp` (Telerik). A `.trdp` is a ZIP:
`definition.xml` (element tree + data bindings) and `invariant.res` (geometry + label
text, `Name.Property "value"`). All geometry below is transcribed from `invariant.res`.

This spec drives the QuestPDF `ThaiUrHemosheetForm` composer and the Angular mirror.

## Global / page

- Paper: A4 portrait, `UnitOfMeasure="Mm"`, snap grid 1mm.
- Report width `8.104in` (~205.8mm). Body panel `panel48` / tables use `8.1in` (~205.7mm).
- Page margins (Telerik report): Left/Right/Top/Bottom `8px` (~2.1mm). We use ~2mm.
- Default font: **Microsoft Sans Serif 7.5pt**, black. Title 18pt bold. Small units 5.5-6pt.
- Default border: **0.4pt solid black** on every cell; adjacent cells drop one side to avoid doubling.
- Section header bar background: **`rgb(192,192,255)` = `#C0C0FF`** (lavender), bold, centered.
- Standard row height: **4.6mm** (0.46cm). Checkbox rows also 4.6mm.
- Thai glyphs appear in a few values (`ไม่มีแพ้ยา`, `สาเหตุการทิ้งตัวกรอง`, `น.`) -> font fallback to Sarabun.

## Region map (top-down)

The body is one `DetailSection` (height 22.242cm) stacked as horizontal bands:

1. `panel48` top band, height 92.02mm, width 8.1in. Split into three columns:
   - Left column `PredialysisAssessmentPanel` (100.03mm wide x 92.02mm):
     - `PreAssTitle` header bar "Predialysys Assessment" (100.03 x 4.6mm, #C0C0FF).
     - Sub-bands inside (Top offsets relative to panel):
       - `TopLeftPanel` (58 x 23.01mm, Top 4.6mm): vitals grid.
         - Row labels col (16mm): BP / PR / RR (via panels) then Urine (13.8mm) / Pain score (18.41mm).
         - `BPValue {BPS}/{BPD}`, unit `mmHg`; `PRValue {HR}` unit `bpm`; `RRValue {RR}` unit `bpm`;
           `BTValue {Temp}` unit `°C`; `SatValue {SpO2}` unit `%`.
         - Urine row: label + Y/N checkboxes + `{ml/day}`; Pain score row: label + Y/N + value + `point`.
       - `TopRightPanel` (42.01 x 41.81mm, Left 58mm, Top 4.6mm): weight grid, label col 24mm + value col 18mm:
         Pre BW, Last BW, Dry weight, Meal/Drink, Weight gain (DW), Target UF, Post BW, Weight loss, IDWG.
       - `VascularAccessPanel` (42.03 x 45.6mm, Left 58mm, Top 46.41mm):
         - `VascularAccessTitle` bar "Vascular Access" (#C0C0FF).
         - Shunt site line; Needle No. `A` / `V` sizes; Priming Vol `A =`/`V =`;
         - Y/N rows: Thrill, Bruit, Edema, Inflamation (each label + Y + N).
       - `BottomLeftPanel` (58 x 64.4mm, Top 27.61mm): symptom checklist, 3 sub-cols:
         label(25mm) | Y(16mm) | N(17mm). Rows (Top step 0.46cm):
         Pale, Edema, Dyspnea, Fever, Crepitatic, Headache, Nausea/Vomitting, Anorexia,
         Itching, Engorged neck vein, Anxiety, Sleep disturbance, Constipation, Prolong bleeding.
   - Right column `panel12` "Hemodialysis Prescription" (4.17in x 92.02mm, Left 10cm):
     - `textBox29` header bar "Hemodialysis Prescription" (#C0C0FF).
     - `panel13` left half (51.02mm): label(17mm)+value(34mm) rows @0.46cm step:
       Machine, Dialyzer, Surface area, Use No., Last TCV, Grade, then
       Test Leak (Pass / Not Pass), Disinfectant (Peracitic acid), Disinfectant test (Pass / Not Pass),
       `สาเหตุการทิ้งตัวกรอง` (reason) box.
     - `panel24` right half (2.16in, Left 5.1cm): mode checkbox `HD` / `Online`; then Yes/No rows:
       Na. Profile, UF. Profile, Isolate; then value rows Na/Na Con off/K+/Ca2+/HCO3(html sub)/Dialysis Flow/Dialysis Temp
       with units mEq/L, ml/min, ℃.
     - `panel35`/`panel36` band (Top under prescription): `Anticoagulant` (No Heparin checkbox) |
       `Time Dialysis` (Time start / Duration Hours / Time off, `น.`), plus Loading/Maintenance lines.

2. Middle band: 3-column nursing plan headers (#C0C0FF): `Nursing Diagnosis` (69.55mm) |
   `Nursing Intervention` (60.85mm) | `Expected Outcomes` (75.44mm), with free-text value rows below.

3. `table1` Dialysis records table (8.1in x rows 4.6mm, header 6mm). Columns with sub-unit line:
   Time | BP (mmHg) | MAP (mmHg) | Pulse (/min) | EBFR (ml/min) | AP (mmHg) | VP (mmHg) |
   TMP (mmHg) | Cond. (mS/cm) | UFR (ml/hr) | Total UF (ml) | Note.
   Data columns ~11.49mm; Time ~10.45mm; Note wide (fills remainder). Header cells #C0C0FF.
   Below the header there is a units sub-row (small 3mm text: mmHg, /min, ml/min, mS/cm, rate, ml, ...).

4. Fluid summary band (left group, boxes 8mm tall, width ~24mm):
   `NSS {sum} ml`, `50% Glucose {sum} ml`, `Extra-fluid {ExtraFluidTotal} ml`,
   `Total fluid replacment {..} ml`, `Total UF {max*1000} ml`, `Net fluid balance {..} ml`.
   Also `Start HD by {signhd}` and `Note` header for the note column.

5. Bottom band `panel51/52` (Top ~9.2cm): four checkbox groups side by side (37-39mm each):
   - `Complication` (checkBox35-45): Hypotension, Hypertension, Muscle cramp, Headache,
     Nausea / Vomitting, Fever, Chest pain, Arrhythmia, Access problem, Hypoglycemia, Dizziness,
     No complication. Plus `Technical complication` (checkBox46-50): Blood leak, Clotted dialyzer,
     Clotted blood line, Machine problem, No complication.
   - `Nursing management` (checkBox51-68): Phycho support, Trenderlenburg position, Monitor V/S,
     Pause UF, Hypertonic solution, Oxygen therapy, Decrease Dialysate T., Hot compress,
     Strength exercise, Cold compress, Aware aspirate, Monitor EKG, Decrease BFR,
     Monitor access flow, Change dialyzer, Change blood line, Notified doctor, Post HD nursing care.
   - `Health education` (checkBox69-75): Nutrition, Vascular Access, Exercise, Personal hygine,
     Medication, Fluid control, KT. Plus Hct:/Hb: value line.
   - `Medication duration HD` (`table3`): columns Name/Dose/Route | Time | Sign.

6. Records tables: `table2` Nurse's note (Time | Content | Sign), `table4`/`table5` doctor / progress,
   Focus | A(Assessment) | I(Intervention) | E(Evaluation) for progress notes.

7. Post Vital band `post vital sign` (Top 7.04cm, width 5.11in): `Post Vital` label + BP {BPS}/{BPD},
   PR {HR}, RR {RR}, Sat {SpO2} %, BT {Temp} °C; and `AVF/AVG` post assessment row
   (post-vas thrill/bruit/hema/sb), Needle No, A/V sizes.

8. Signatures footer:
   - `Dialysis Nurse` : `{NursesInShiftNonPN}` (textBox187/188, Top 8.42cm)
   - `Dialysis NA` : `{NursesInShiftPN}` (textBox185, Top 8.88cm)
   - `Nephrologist {DoctorName}` (textBox157, Top 8.74cm)

## Data bindings (key)

- Patient: `Patient.Name`, `Patient.HN` (shown as CN), `Patient.Age`, `Patient.IdentityNo` (ID Card NO.),
  `Patient.Coverage`, `Patient.AllergiesText` (default `ไม่มีแพ้ยา`), `Patient.UnderlyingDisplay` (Diagnosis).
- Header meta: `CycleStartTime` (Date), `TreatmentNo` (HD NO.).
- Dehydration: PreWeight, LastPostWeight, FoodIntakeWeight, PostWeight, TotalUF, ExtraFluidTotal.
- Prescription: Dialyzer, DialyzerSurfaceArea, Na, DialysateK, DialysateCa, HCO3, DialysateFlowRate,
  DialysateTemperature, Duration.Hours, InitialAmountMl/InitialAmount (Loading),
  MaintainAmountMl/MaintainAmount (Maintenance).
- Assessments: `Hemo.Get(Assessments.Pre|Re|Post, "<key>")` .Checked/.Value/.Text; keys include
  thrill, bruit, vas:edema, pain, urine, reason, and symptom keys.
- Dialysis rows: Timestamp, BPS/BPD, MAP, HR, BFR, AP, VP, TMP, DC(Cond.), UFRate, UFTotal, NSS, Note.
- Labs: `Hemo.GetLab(Labs,"hct"|"hb")`.

## Notes / non-obvious

- Weight gain = PreWeight - DryWeight; Weight loss = |PreWeight - PostWeight| (prefix `+` if gained);
  IDWG = PreWeight - LastPostWeight; Net fluid balance = maxUF - NSS - Glucose50 - ExtraFluid.
- Checkbox images use `checkbox-x13.png` / `checkbox-checked-x13.png` (ThaiUR uses x13 size).
- The whole thing must fit ONE A4 page (dense). Row heights are fixed (4.6mm) - do not let content grow.
