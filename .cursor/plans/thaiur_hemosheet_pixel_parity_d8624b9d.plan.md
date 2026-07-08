---
name: ThaiUR Hemosheet Pixel Parity
overview: ปรับ report Template04 (Hemosheet) ในระบบใหม่ (Hemo-PDF, QuestPDF + Angular viewer) ให้หน้าตาตรงกับต้นฉบับ Telerik `Hemosheet-ThaiUR.trdp` แบบ pixel-perfect โดยยังคงสถาปัตยกรรม block/section ที่ยืดหยุ่น และทำให้ทั้ง 2 renderer (PDF + preview บนจอ) ตรงกัน
todos:
  - id: spec
    content: "แตก Hemosheet-ThaiUR.trdp (definition.xml + invariant.res) เป็นเอกสาร spec อ้างอิง: ระบุแต่ละ section/panel พร้อมพิกัด (Left/Top/Width/Height เป็น mm), label จริง (EN/TH), สี (#C0C0FF header, #14224D title), ฟอนต์/ขนาด (MS Sans Serif 7.5pt/18pt), เส้นขอบ 0.4pt, รายการ checkbox ทั้ง Y/N และกลุ่มล่าง 4 กลุ่ม, คอลัมน์ตารางบันทึกฟอก (Time/BP/MAP/Pulse/EBFR/AP/VP/TMP/Cond./UFR/Total/Note). เก็บเป็น .cursor/docs/hemosheet-thaiur-spec.md แล้วแบ่งเป็น grid 2 คอลัมน์"
    status: completed
  - id: style-profile
    content: "เพิ่มชั้น style profile: refactor PdfSectionMetrics/PdfStyleDefaults ให้รับค่าต่อ profile (border 0.4pt, header #C0C0FF, title #14224D, font MS Sans Serif 7.5pt) และเพิ่ม style-override fields ใน ReportBlock/section signatures โดยไม่กระทบ Default; register ฟอนต์ target + fallback ไทย ใน FontRegistration; ตั้งค่า page margins/density ให้พอดี 1 หน้า"
    status: completed
  - id: blocks
    content: "เพิ่ม/ปรับ block types + variants ที่ ThaiUR ต้องใช้ ใน ReportBlock.cs และ ReportBlockPdfComposer.cs: assessment+Y/N checkbox pair panel (ซ้าย), prescription panel ที่มี inline checkbox (HD/Online, Yes/No), 3-คอลัมน์ Nursing Diagnosis/Intervention/Expected Outcomes, ตารางบันทึกฟอกที่มีคอลัมน์ MAP/Cond., แถวสรุปน้ำ (NSS/50% glucose/Extra-fluid/Total UF/Net balance), กลุ่ม checkbox ล่าง 4 กลุ่ม (Complication/Nursing management/Health education/Medication duration HD), Post Vital row, signature footer (Nephrologist/Dialysis Nurse/NA)"
    status: completed
  - id: thaiur-layout
    content: "สร้าง ThaiUr branch ใน HemosheetLayoutPlanner + section renderers + HemosheetPreviewMappers: จัด grid 2 คอลัมน์ตาม spec, ใช้ label EN, header 'Hemodialysis Record' 18pt + โลโก้ TRD + บล็อกข้อมูลผู้ป่วยขวา, แถว Diagnosis/Drug Allergy; ลงทะเบียน ThaiUr ใน HemosheetLayoutProfileRegistry; เพิ่ม mock data assets/mock-data/template-04-hemosheet-thaiur.json"
    status: completed
  - id: angular-parity
    content: "Mirror ฝั่ง Angular viewer: เพิ่ม/แก้ block components ใน client/projects/hemo-report-viewer/src/lib/components/blocks/* + report-viewer.scss + header/footer components ให้รองรับ style profile และ block ใหม่, แล้วรัน scripts/sync-report-viewer.mjs เพื่อ sync เข้า Hemo-frontend"
    status: completed
  - id: verify
    content: "ตรวจ pixel-parity: generate PDF จาก mock ThaiUR แล้วเทียบซ้อนกับภาพต้นฉบับ, tune ค่า (สัดส่วนคอลัมน์/ความสูงแถว/ระยะ/สี) จนตรง, เช็ค preview บนจอตรงกับ PDF, เพิ่ม integration test ใน Hemo.Pdf.Integration.Tests"
    status: completed
isProject: false
---

# ThaiUR Hemosheet Pixel Parity

## เป้าหมาย
ทำให้ report `template-04-hemosheet` **profile ThaiUr** ออกมาเหมือน `Hemosheet-ThaiUR.trdp` แบบ pixel-perfect (ฟอร์ม "Hemodialysis Record" อัดแน่น 1 หน้า) โดยยึด ThaiUR เป็น baseline ก่อน แล้วจึงแตกไป Default/tenant อื่นภายหลัง ต้องคงความยืดหยุ่นของ block/section เดิม และทำให้ PDF (QuestPDF) กับ preview (Angular viewer) หน้าตาตรงกัน

## ช่องว่างหลัก (ปัจจุบัน vs ThaiUR)
- สีแถบหัวข้อ: ปัจจุบัน `#dce6f2` (ฟ้าอ่อน) ตัวอักษรดำ -> ThaiUR ใช้ **`rgb(192,192,255)` = #C0C0FF** (ลาเวนเดอร์)
- ฟอนต์: ปัจจุบัน Sarabun -> ThaiUR ใช้ **Microsoft Sans Serif** (ส่วนใหญ่ 7.5pt, title 18pt) + fallback ไทย
- เส้นขอบ: ปัจจุบัน `0.5f` hardcode ทุก section -> ThaiUR **0.4pt**
- density: ปัจจุบัน block-flow กระจาย 2 หน้า -> ThaiUR grid 2 คอลัมน์ อัดแน่น 1 หน้า
- ลำดับ/label section, ตาราง (มีคอลัมน์ MAP, Cond., Nursing Diagnosis/Intervention/Outcomes) และกลุ่ม checkbox ล่าง 4 กลุ่ม แตกต่างจากที่ [`HemosheetLayoutPlanner`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Layouts/Hemosheet/HemosheetLayoutPlanner.cs) สร้างอยู่

## แนวทางสถาปัตยกรรม (คงความยืดหยุ่น)
1. คงรูปแบบ "map once, render both": data -> `ReportBlock` -> ทั้ง QuestPDF ([`ReportBlockPdfComposer`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Sections/Content/ReportBlockPdfComposer.cs)) และ Angular viewer
2. เพิ่มชั้น **style profile / per-block style override** เพื่อให้ ThaiUR กำหนดสี/เส้น/ฟอนต์/padding ต่างจาก Default ได้ โดยไม่กระทบ template อื่น (ปัจจุบันสไตล์ถูก hardcode รวมศูนย์ที่ [`PdfSectionMetrics`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Constants/PdfSectionMetrics.cs)/[`PdfStyleDefaults`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Constants/PdfStyleDefaults.cs))
3. เพิ่ม **ThaiUr branch** ใน planner + section renderers เพื่อจัด layout 2 คอลัมน์ตามฟอร์ม โดยใช้ block ที่มี/เพิ่มใหม่ (ไม่ใช้ absolute positioning; ใช้ nested table + column weight + fixed row height ที่แปลงจากค่า mm ของ .trdp -> ได้ผลตรงตาแต่ยัง maintain ได้)
4. Mirror ทุกการเปลี่ยนแปลงใน Angular viewer + `report-viewer.scss` แล้ว sync ด้วย `scripts/sync-report-viewer.mjs`

```mermaid
flowchart LR
  data[Hemosheet DTO] --> planner["ThaiUr planner branch"]
  planner --> blocks["ReportBlock + styleProfile"]
  blocks --> pdf["QuestPDF (PDF จริง)"]
  blocks --> json["ReportDocument JSON"]
  json --> ng["Angular viewer + scss"]
  pdf --> parity{"เทียบกับ .trdp"}
  ng --> parity
```

## ไฟล์อ้างอิงหลัก
- Engine: [`HemosheetLayoutPlanner.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Layouts/Hemosheet/HemosheetLayoutPlanner.cs), [`HemosheetLayoutProfileRegistry.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Layouts/Hemosheet/HemosheetLayoutProfileRegistry.cs), [`Renderers/HemosheetSectionRenderers.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Layouts/Hemosheet/Renderers/HemosheetSectionRenderers.cs), [`Renderers/HemosheetParitySectionRenderers.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Layouts/Hemosheet/Renderers/HemosheetParitySectionRenderers.cs)
- Block model + mappers: [`ReportBlock.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Models/Preview/ReportBlock.cs), `HemosheetPreviewMappers.cs`
- Style/geometry: [`PdfSectionMetrics.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Constants/PdfSectionMetrics.cs), [`PdfStyleDefaults.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Constants/PdfStyleDefaults.cs), [`ReportPageLayout.cs`](d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Core/Constants/ReportPageLayout.cs), `FontRegistration.cs`
- Target ต้นฉบับ: `d:/GoodRepo/Hemo-Report/Hemosheet-ThaiUR.trdp` (ภายใน: `definition.xml` = layout, `invariant.res` = พิกัด/label)
- Angular viewer: `d:/GoodRepo/Hemo-PDF/client/projects/hemo-report-viewer/src/lib/components/blocks/*`

## หมายเหตุ/ข้อควรระวัง
- "pixel-perfect" ในกระดาษ flow-based (QuestPDF) จะทำโดยจับสัดส่วน/ความสูงแถวให้ตรง ไม่ใช่ absolute x/y ของ Telerik — ต่างจริงระดับ sub-mm อาจมี ผมจะ tune จนตาแยกไม่ออก
- ฟอนต์ Microsoft Sans Serif เป็นฟอนต์ Windows หากรันบน Linux ต้อง embed ttf ที่เทียบเท่า (หรือ Arial/Liberation Sans) + Sarabun/Noto ไว้รองรับไทย — จะยืนยันฟอนต์ที่ฝังตอน implement
- ทุกการปรับ block ต้องแก้ทั้งฝั่ง C# และ Angular เพื่อให้ preview กับ PDF ตรงกัน