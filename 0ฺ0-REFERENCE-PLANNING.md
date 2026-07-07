# สรุประบบสร้าง PDF ของโปรเจกต์ NSS

> เอกสารนี้สรุปจากการสำรวจโค้ดใน repo `NSS` เพื่อใช้เป็นแนวทางสำหรับโปรเจกต์ `Hemo-PDF`

---

## ภาพรวม

NSS สร้าง PDF **ฝั่ง Backend เท่านั้น** โดยใช้ไลบรารี **QuestPDF** (.NET)  
Frontend **ไม่ render PDF เอง** — แค่เรียก API แล้วรับ `application/pdf` เป็น Blob มาเปิดในแท็บใหม่หรือดาวน์โหลด

เอกสารหลักใน NSS:
- `REPORT_SYSTEM_GUIDE.md` — สถาปัตยกรรมและแนวปฏิบัติ
- `backend/NikkisoServiceAPI/Documentation/README-Reports-QuestPDF.md` — รายละเอียดเชิงเทคนิค

---

## สถาปัตยกรรมโดยรวม

```
┌─────────────────────────────────────────────────────────────┐
│  Frontend (Next.js)                                         │
│  pdf-utils.ts → authFetch → GET /api/service-reports/{id}/pdf│
│  use-pdf.ts (hook) → toast error, loading state             │
└──────────────────────────┬──────────────────────────────────┘
                           │ PDF bytes (Blob)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Backend API (.NET 8)                                       │
│  ServiceReportsController.GeneratePdf()                       │
│    → ReportKindResolver (เลือกประเภทรายงาน)                 │
│    → ReportFactory (Factory Pattern)                          │
│    → IReportRenderer (ตาม ReportKind)                         │
│         1. DataProvider  — โหลดข้อมูลจาก DB + Template       │
│         2. Composer      — จัด layout ด้วย QuestPDF Fluent   │
│         3. IPdfRenderer  — QuestPdfRenderer → byte[]        │
└─────────────────────────────────────────────────────────────┘
```

### Pattern หลัก: Factory / Strategy + Separation of Concerns

แต่ละประเภทรายงานมี 3 ชิ้นส่วน:

| ชิ้นส่วน | Interface | หน้าที่ |
|----------|-----------|---------|
| Data Provider | `IReportDataProvider` | โหลด ServiceReport, Template Snapshot/Current, สร้าง ViewModel |
| Composer | `ILayoutComposer` | สร้าง `QuestLayout` ด้วย QuestPDF Fluent API |
| Report Renderer | `IReportRenderer` | ประสาน 3 ขั้นตอน: GetData → Compose → Render |

---

## API Endpoint

```
GET /api/service-reports/{serviceReportId}/pdf?overrideKind={ReportKind}
```

| รายการ | รายละเอียด |
|--------|------------|
| Controller | `ServiceReportsController.GeneratePdf()` |
| Auth | `SERVICE_REPORT_READ_ALL` / `READ_TEAM` / `READ_OWN` |
| Rate Limit | Policy `"PdfGeneration"` — 10 requests/min ต่อ user/IP |
| Response | `Content-Type: application/pdf`, filename `service-report-{id}.pdf` |
| Query `overrideKind` | บังคับใช้ ReportKind (optional) แทน auto-resolve |

### ขั้นตอนใน Controller

1. โหลด ServiceReport พร้อม includes
2. ตรวจสอบสิทธิ์เข้าถึงรายงาน
3. Resolve `ReportKind` จาก `overrideKind` หรือ `ReportKindResolver`
4. สร้าง `ReportContext` พร้อม `Parameters["ServiceReportId"]`
5. เรียก `renderer.RenderReportAsync()` → คืน `File(pdfBytes, "application/pdf", ...)`

---

## ประเภทรายงาน (ReportKind)

```csharp
public enum ReportKind
{
    InstallPmDialysis,  // เครื่อง Dialysis — Install/PM
    InstallPmCCDS,      // เครื่อง CCDS — Install/PM
    InstallPmRO,        // เครื่อง RO — Install/PM
    Standby,            // Standby service
    Repair,             // งานซ่อม
    RO                  // RO standalone
}
```

### กฎการเลือก ReportKind (`ReportKindResolver`)

| เงื่อนไข | ReportKind |
|----------|------------|
| `JobType.RO` หรือ `JobCategory.RO` | `RO` |
| `JobCategory.STANDBY` หรือ `ReportType.STANDBY` | `Standby` |
| `JobCategory.REPAIR` หรือ `ReportType.REPAIR` | `Repair` |
| Install/PM + `MachineType.DIALYSIS` | `InstallPmDialysis` |
| Install/PM + `MachineType.CCDS` | `InstallPmCCDS` |
| Install/PM + `MachineType.RO` | `InstallPmRO` |
| อื่น ๆ (fallback) | `InstallPmDialysis` |

ลำดับความสำคัญของ machine type: **Template Snapshot** → **Report.Machine.Type**

---

## Backend — โครงสร้างไฟล์

```
backend/NikkisoServiceAPI/
├── Controllers/
│   └── ServiceReportsController.cs          # endpoint PDF
├── Extensions/
│   └── FeatureServiceExtensions.cs          # DI registration
├── Services/Reports/
│   ├── Core/
│   │   ├── ReportFactory.cs
│   │   ├── ReportKindResolver.cs
│   │   ├── ReportKind.cs
│   │   ├── ReportContext.cs
│   │   ├── ReportStyleDefaults.cs           # font/size constants
│   │   ├── ReportTemplateLoader.cs
│   │   ├── ReportTemplateSnapshotParser.cs
│   │   ├── ChecklistItemValueResolver.cs
│   │   ├── IPdfRenderer.cs
│   │   ├── IReportRenderer.cs
│   │   ├── IReportDataProvider.cs
│   │   └── ILayoutComposer.cs
│   ├── Layouts/
│   │   ├── InstallPmDialysis/  (DataProvider, Composer, ReportRenderer)
│   │   ├── InstallPmCCDS/
│   │   ├── InstallPmRO/
│   │   ├── Standby/
│   │   ├── Repair/
│   │   └── RO/
│   ├── Renderers/Quest/
│   │   ├── QuestPdfRenderer.cs              # QuestPDF implementation
│   │   └── QuestLayout.cs                   # layout wrapper
│   └── Sections/
│       ├── DefaultReportHeaderSection.cs
│       ├── DefaultReportFooterSection.cs
│       └── ReportSectionResolver.cs
└── Fonts/sarabun/                           # ฟอนต์ไทย Sarabun
```

### QuestPDF Renderer (`QuestPdfRenderer`)

- Package: `QuestPDF` v2024.7.2
- ลงทะเบียนฟอนต์ **Sarabun** จาก `Fonts/sarabun/Sarabun-*.ttf`
- ขนาดหน้า A4, margin ปรับได้ผ่าน `QuestLayout`
- `GeneratePdf()` เป็น synchronous → ห่อด้วย `Task.Run()` + `CancellationToken`
- จำกัดขนาด PDF สูงสุด **50 MB**
- Default footer: เลขหน้า `current / total`

### Styling (`ReportStyleDefaults`)

- ฟอนต์หลัก: Sarabun (Light, ExtraLight, SemiBold)
- ขนาดตัวอักษร: Header 8–14pt, Body 7.5pt, Footer 6pt

### Template Data

- ถ้ามี `TemplateSnapshotId` → โหลดจาก Snapshot (~99% ของกรณี)
- ถ้าไม่มี → fallback โหลดจาก Current Template ตาม `MachineType`

---

## Frontend — โครงสร้างและการใช้งาน

### ไฟล์หลัก

| ไฟล์ | หน้าที่ |
|------|---------|
| `frontend/lib/pdf-utils.ts` | สร้าง URL, fetch PDF blob, open/download |
| `frontend/hooks/use-pdf.ts` | React hook — loading state + toast error |

### Flow ใน `pdf-utils.ts`

1. `buildPdfUrl(id, overrideKind?)` → `{apiUrl}/api/service-reports/{id}/pdf`
2. `fetchPdfBlob(url)` → `authFetch` (พร้อม JWT) + offline detection
3. `openServiceReportPdf(id)` → สร้าง Blob URL → `window.open()`
4. `downloadServiceReportPdf(id)` → สร้าง `<a download>` (ยังไม่ถูกใช้ใน UI)

### จุดที่เรียกใช้ PDF ใน UI

| หน้า/Component | พฤติกรรม |
|----------------|----------|
| `service-reports/[id]/page.tsx` | ปุ่ม Generate PDF ใน ViewMode (หลัง sign แล้ว) |
| `service-reports/components/ViewMode.tsx` | UI section "Service Report PDF" — ต้อง online |
| `service-reports/components/ServiceReportsTable.tsx` | ไอคอน PDF — เฉพาะ status `SIGNED` |
| `components/jobs/job-subjobs.tsx` | ปุ่ม PDF ในรายการ sub-job reports |
| `service-reports/new/page.tsx` | placeholder `onOpenPdf={() => {}}` (ยังไม่ implement) |

### เงื่อนไขการเปิด PDF (Frontend)

- รายงานต้องมี status **`SIGNED`** ก่อน
- ต้อง **online** (ตรวจด้วย `isOnline` / health check)
- ใช้ `authFetch` — ต้อง login และมี permission ที่ถูกต้อง

### หมายเหตุ: `reports/page.tsx`

หน้า Reports ใช้ endpoint แยก `/api/reports/{id}/download` ซึ่ง **ไม่พบใน backend ปัจจุบัน** — น่าจะเป็น legacy/stub ที่ยังไม่เชื่อมต่อ

---

## Dependency Injection

ลงทะเบียนใน `FeatureServiceExtensions.AddFeatureServices()`:

```csharp
services.AddSingleton<IPdfRenderer, QuestPdfRenderer>();
services.AddScoped<IReportKindResolver, ReportKindResolver>();

// แต่ละ layout: DataProvider + Composer + ReportRenderer (Transient)
services.AddScoped<IReportFactory>(sp => new ReportFactory(items, sp, typeof(StandbyReportRenderer)));
```

Fallback renderer: `StandbyReportRenderer`

---

## การทดสอบ

| ไฟล์ | ประเภท |
|------|--------|
| `ReportGenerationIntegrationTests.cs` | Integration — end-to-end HTTP PDF |
| `RepairDataProviderTests.cs` | Unit — data provider |
| `StandbyDataProviderTests.cs` | Unit — data provider |
| `ChecklistItemValueResolverTests.cs` | Unit — checklist parsing |
| `ReportTemplateSnapshotParserTests.cs` | Unit — snapshot parsing |

Test cases สำคัญ:
- Generate PDF สำหรับ Standby report → ได้ PDF bytes
- Report ไม่มี → 404
- `overrideKind` → ใช้ kind ที่ระบุ
- ไม่มี auth → 401

---

## Performance & ข้อจำกัด

| รายการ | ค่าโดยประมาณ |
|--------|-------------|
| เวลารวม | ~225–2120 ms |
| PDF generation | ~200–2000 ms (CPU-intensive) |
| Rate limit | 10 req/min ต่อ user |
| Max PDF size | 50 MB |

ข้อจำกัด QuestPDF:
- `GeneratePdf()` ไม่รองรับ async โดยตรง → ใช้ `Task.Run()`
- ไม่มี PDF caching (แนะนำในอนาคต)

---

## แนวทางสำหรับ Hemo-PDF

จากแพทเทิร์นของ NSS สิ่งที่ควรพิจารณาเมื่อเริ่มโปรเจกต์ใหม่:

### 1. สถาปัตยกรรมที่แนะนำ (ตาม NSS + ขยาย Preview)

- **Server-side PDF generation** ด้วย QuestPDF — เหมือน NSS (print / download คุณภาพเต็ม)
- แยก **DataProvider / Composer / Renderer** ต่อประเภทเอกสาร
- ใช้ **Factory Pattern** สำหรับเลือก renderer
- **Preview บนจอ** — แยกจาก NSS: ไม่ parse PDF ฝั่ง browser แต่ใช้ **ReportDocument JSON → HTML/CSS** (แนวเดียวกับ Telerik `PRINT_PREVIEW` ใน Hemopro) — ดู [02-FEATURE-PREVIEW-PDF.md](./02-FEATURE-PREVIEW-PDF.md)

```
                    ┌── POST /api/pdf/generate ──► QuestPDF ──► application/pdf (print/download)
DTO + template ────┤
                    └── POST /api/report/preview ──► ReportDocument JSON ──► @hemo/report-viewer (หน้าจอ)
```

### 2. สิ่งที่ต้องมี (สถานะ Phase 0–5)

- [x] API endpoint คืน `application/pdf`
- [x] Auth + tenant middleware (mock dev / JWT scaffold)
- [x] Rate limiting สำหรับ PDF generation
- [ ] ฟอนต์ไทย (Sarabun) — ไฟล์ยังไม่ copy ลง `assets/fonts/sarabun/`
- [x] `CancellationToken` support + max PDF 50MB
- [x] Integration tests สำหรับ PDF endpoint
- [x] **Report Preview** — `ReportDocument` schema + `POST /api/report/preview` + `@hemo/report-viewer` (Phase 6 ✅)
- [ ] **Hemopro integration** — Hemosheet report-data API + dedicated layout (Phase 7 🔄)

### 3. สิ่งที่ปรับจาก NSS / เพิ่มเติมใน Hemo-PDF

| หัวข้อ | NSS | Hemo-PDF |
|--------|-----|----------|
| Preview UI | เปิด PDF blob ในแท็บใหม่ | **ReportDocument + HTML viewer** (แทน Telerik `tr-viewer`) |
| Data source | DataProvider query DB | Caller ส่ง DTO (stateless) |
| Frontend client | `pdf-utils.ts` (open/download) | `@hemo/pdf-client` + `@hemo/report-viewer` |
| Template count | 6 ReportKind | 12 ReportTemplateId |

- เพิ่ม PDF result caching (optional)
- Dedicated layout ต่อ template (hemosheet ฯลฯ) — คู่กับ preview blocks

### 4. เปรียบเทียบ Preview: Telerik (Hemopro ปัจจุบัน) vs Hemo Report Viewer

| | Telerik `tr-viewer` | `@hemo/report-viewer` (แผน) |
|--|---------------------|------------------------------|
| Input | `.trdp` definition + parameters | `ReportDocument` JSON จาก pipeline เดียวกับ PDF |
| Render | Report.Api → HTML/CSS | Angular components + CSS (mirror `PdfStyleDefaults`) |
| Toolbar | Kendo viewer (zoom, หน้า, print) | `HemoReportToolbarComponent` — ทำเฉพาะที่ต้องการ |
| Print คุณภาพเต็ม | export PDF จาก Telerik | `POST /api/pdf/generate` (QuestPDF) |
| Dependency | Telerik license + jQuery + Kendo | ~20KB Angular components + Sarabun web font |
| WYSIWYG | ~95–100% (engine เดียวกัน) | ~95% บนจอ; print ใช้ PDF จริง |

**เหตุผลที่ไม่ใช้ iframe/pdf.js:** รายงาน Hemopro เป็น structured form (header, ตาราง, checkbox) — map ตรงกับ `Hemo.Pdf.Sections` ได้ ไม่จำเป็นต้อง parse PDF binary

### 5. Tech Stack อ้างอิง

| Layer | NSS | Hemo-PDF |
|-------|-----|----------|
| PDF Library | QuestPDF 2024.7.2 | QuestPDF 2024.7.2 |
| Backend | .NET 8 | .NET 8 (`Hemo.Pdf.Api` :5090) |
| Preview | — | ReportDocument JSON + Angular HTML/CSS |
| PDF client | Next.js `pdf-utils.ts` | `@hemo/pdf-client` |
| Preview client | — | `@hemo/report-viewer` |
| Font | Sarabun (TTF) | Sarabun (TTF + `@font-face` ใน viewer) |
| Pattern | Factory + Strategy | Factory + Strategy + dual output (PDF / Preview) |

---

## รายการไฟล์สำคัญ (Quick Reference)

### Backend
- `Controllers/ServiceReportsController.cs` — PDF endpoint
- `Services/Reports/Core/ReportFactory.cs`
- `Services/Reports/Core/ReportKindResolver.cs`
- `Services/Reports/Renderers/Quest/QuestPdfRenderer.cs`
- `Extensions/FeatureServiceExtensions.cs` — DI
- `Extensions/InfrastructureServiceExtensions.cs` — rate limit policy

### Frontend
- `lib/pdf-utils.ts` — core PDF client utilities
- `hooks/use-pdf.ts` — React hook
- `app/(main)/service-reports/[id]/page.tsx` — หน้ารายละเอียด + Generate PDF
- `app/(main)/service-reports/components/ViewMode.tsx` — UI ปุ่ม PDF
- `app/components/jobs/job-subjobs.tsx` — PDF จากหน้า Job

### Documentation & Tests
- `REPORT_SYSTEM_GUIDE.md`
- `NikkisoServiceAPI.Tests/Integration/Reports/ReportGenerationIntegrationTests.cs`

### Hemo-PDF (เพิ่มเติมจาก NSS)
- [02-FEATURE-PREVIEW-PDF.md](./02-FEATURE-PREVIEW-PDF.md) — Report Preview feature spec
- [01-IMPLEMENT-PLANNING.md](./01-IMPLEMENT-PLANNING.md) — แผนรวม Phase 0–6
