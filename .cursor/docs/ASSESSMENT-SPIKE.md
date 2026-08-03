# Assessment Spike — Decision (HEM-1074)

> **วันที่:** 2026-08-03  
> **ขอบเขต:** Default `Hemosheet.trdp` AssessmentTable vs Hemo-PDF `checklist-table`  
> **สถานะ:** ตัดสินใจแล้ว — **Hybrid (C)**  
> Process: [LAYOUT-FIDELITY-PROCESS.md](./LAYOUT-FIDELITY-PROCESS.md)

---

## คำตัดสินใจ

| ทางเลือก | สรุป | เลือก? |
|----------|------|--------|
| A — ใช้แค่ `checklist-table` | พอสำหรับ ThaiUR / AVF / footer | ไม่พอสำหรับ Default Pre∪Re |
| B — สร้าง `assessment-matrix` ทั่วไป (topic×ทุก option) | Overkill — Telerik ไม่ได้ทำแบบนั้น | ไม่เลือก |
| **C — Hybrid** | คง checklist ส่วนใหญ่ + เพิ่ม **Pre∪Re topic matrix** เฉพาะ Default | **✅ เลือก** |

**ความหมายของ “matrix” ใน `.trdp`:** ตาราง Topic \| Assessment(Y/N) \| Reassessment(Y/N) — **ไม่ใช่** grid ตัวเลือกแบบไดนามิก

---

## หลักฐานสั้น ๆ

### BE ส่งอะไรวันนี้

`HemosheetAssessmentItemDto`: `Name`, `Checked`, `Text`, `SelectedOptions` (DisplayName ของ option)  
กลุ่ม: `Pre` / `Re` / `Post` / `Other`

- Pre = type Pre && !IsReassessment  
- Re = type Pre && IsReassessment  
- Post multi-select (เช่น `complication`) = หนึ่ง item + `SelectedOptions[]`  
- Metadata / OptionMetadata ที่ Telerik ใช้ **ยังไม่อยู่ใน report DTO**

### Telerik Default (`Hemosheet.trdp`)

- `AssessmentTable` hardcode keys: `pain`, `chest`, `dyspnea`, …  
- คอลัมน์: Topic | Pre Y/N | Re Y/N (Re ขึ้นกับ parameter reassessment)  
- Post/footer = checkbox กลุ่ม `GetSelect(Post, "complication", "hypo")` ฯลฯ  
- **Other** ไม่ bind ใน default trdp

### ThaiUR

คนละฟอร์ม: Pre Y/N รายการเดียว (ไม่มี Re คอลัมน์) + footer 4 กลุ่ม — ใช้ checklist / ThaiUr composer ได้ ไม่ต้อง AssessmentTable

### Hemo-PDF ปัจจุบัน

- Pre ใน TopLayout เป็น `yn-columns`  
- Re/Post/Other เป็น checklist แยก  
- `MapFooterChecklists` คาด dotted names (`complication.*`) แต่ BE ส่ง parent name + `SelectedOptions` → **mismatch**  
- `FormatAssessmentLabel` ถ้ามี SelectedOptions จะใช้ join เป็น label (กลืนชื่อ topic)

---

## แผน implement (หลัง spike)

### Phase A — Mapper / PDF / viewer (ทำก่อน, ไม่ต้องรอ BE เต็ม)

1. แก้ `HemosheetPreviewMappers`:
   - ขยาย Post multi-select → หนึ่งแถวต่อ option  
   - แก้ footer filter ให้รองรับ BE shape  
   - join Pre+Re by `Name` → layout `pre-re-matrix` (หรือ block ใหม่)
2. ขยาย `checklist-table` layout **หรือ** เพิ่ม `assessment-matrix` (ReportBlock + QuestPDF + Angular)
3. Planner: Default ใช้ Pre∪Re matrix; ThaiUR คง yn/footer; กัน double-render Pre
4. อัปเดต mock JSON ให้ตรง BE
5. Unit tests mapper + planner

### Phase B — BE contract (เมื่อต้องการ empty-form / label ถูกต้อง)

| Field | เหตุผล |
|-------|--------|
| `DisplayName` บน item | คอลัมน์ Topic ไม่โชว์ raw `pain` |
| Option **Name** keys (ไม่ใช่แค่ DisplayName) | เทียบ `GetSelect` / filter เสถียร |
| Catalog / emit ทุก master row (unchecked) | ฟอร์มเปล่าเหมือน Telerik |
| `Value` (optional) | คะแนนปวด ThaiUR |

`Text` map อยู่แล้ว — ไม่บล็อกรอบแรก

### Profile split

| Profile | Assessment UI |
|---------|----------------|
| Default / Rama / New / CAH | Pre∪Re topic matrix + Post option checklists |
| ThaiUr | yn checklist + footer clusters / ThaiUr form |

---

## เกณฑ์ผ่าน spike → implement

- [x] อ่าน AssessmentTable จริงจาก `.trdp`  
- [x] เทียบกับ DTO + mapper ปัจจุบัน  
- [x] ตัดสิน A/B/C  
- [x] Implement Phase A (mapper + matrix layout)  
- [ ] Visual compare mock Default vs Telerik Assessment panel  
- [ ] Phase B BE contract gaps (DisplayName / option keys / catalog)

---

## ไฟล์หลักที่จะแตะใน Phase A

| Repo | Path |
|------|------|
| hemo-pdf | `HemosheetPreviewMappers.cs` |
| hemo-pdf | `ChecklistTablePreviewMapper.cs` / `ChecklistTableSection.cs` |
| hemo-pdf | `HemosheetLayoutPlanner.cs` |
| hemo-pdf | Angular `checklist-table-block` (+ sync) |
| hemo-pdf | `assets/mock-data/template-04-hemosheet-*.json` |
| hemo-back (Phase B) | `HemosheetReportDto` / `HemosheetAssessmentMapping` |
