# Hemo-PDF

บริการสร้าง PDF แยกสำหรับ **HemodialysisPro** — Standalone ASP.NET Core API + Angular client library

## Quick Start

```bash
# Run API (port 5090)
cd src/Hemo.Pdf.Api
dotnet run

# Test
curl http://localhost:5090/health

curl -X POST http://localhost:5090/api/pdf/generate \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Code: tenant-demo-a" \
  -H "Authorization: Bearer dev" \
  -d '{
    "reportTemplateId": "template-01-dialysis-session",
    "tenantCode": "tenant-demo-a",
    "entityId": "session-1",
    "data": {
      "patientName": "สมชาย ใจดี",
      "patientId": "HN-001234",
      "sessionDate": "2026-07-06"
    }
  }' --output test.pdf

curl -X POST http://localhost:5090/api/report/preview \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Code: tenant-demo-a" \
  -H "Authorization: Bearer dev" \
  -d '{
    "reportTemplateId": "template-02-lab-result",
    "tenantCode": "tenant-demo-a",
    "entityId": "test-1",
    "data": { "patientName": "Test Patient", "value": 42 }
  }'
```

Swagger: `http://localhost:5090/swagger` → `POST /api/report/preview` หรือ `/api/pdf/generate`

**Browser demo (ไม่ต้องมี Angular):** เปิด `client/demo/report-preview-demo/index.html` หลังรัน API

```bash
# Run all tests
dotnet test Hemo.Pdf.sln
```

```bash
# Docker
docker compose up --build
```

## Architecture

```
Angular (@hemo/pdf-client + @hemo/report-viewer)
    │ POST /api/report/preview  → ReportDocument JSON (preview)
    │ POST /api/pdf/generate    → application/pdf (print)
    ▼
Hemo.Pdf.Api (Standalone :5090)
    ├── ReportPreviewController / PdfController
    ├── TenantMiddleware / MockAuth (dev)
    └── Hemo.Pdf.Application
            ├── ReportPreviewService / PdfGenerationService
            ├── Branding (JSON per tenant)
            ├── Sections (Header/Footer/Content + Preview mappers)
            └── Layouts (12 templates → QuestPDF + ReportDocument)
```

## Solution Structure

```
src/
├── Hemo.Pdf.Api/           # Standalone Web API
├── Hemo.Pdf.Application/   # DI, guards, orchestration
├── Hemo.Pdf.Core/          # Contracts & models
├── Hemo.Pdf.Branding/      # Tenant branding store
├── Hemo.Pdf.Sections/      # Reusable PDF components
├── Hemo.Pdf.Layouts/       # 12 report templates
└── Hemo.Pdf.Rendering/     # QuestPDF

client/projects/hemo-pdf-client/      # @hemo/pdf-client (print/download)
client/projects/hemo-report-viewer/   # @hemo/report-viewer (HTML preview)
client/demo/report-preview-demo/      # Static browser demo
assets/branding/                   # tenant-demo-a.json, tenant-demo-b.json
assets/mock-data/                  # Sample DTOs
tests/                             # Unit + integration tests
```

## Report Templates (12)

| ID | Requires Sign |
|----|---------------|
| `template-01-dialysis-session` | Yes (dedicated layout) |
| `template-02-lab-result` … `template-12-summary` | See `ReportTemplates.cs` |

## Angular Integration

```typescript
// app.config.ts or module providers
import { HEMO_PDF_CONFIG } from '@hemo/pdf-client';

providers: [
  {
    provide: HEMO_PDF_CONFIG,
    useValue: {
      pdfApiUrl: 'http://localhost:5090',
      getAuthToken: () => localStorage.getItem('token'),
      getTenantCode: () => currentTenantCode,
    },
  },
]
```

```typescript
import { HemoPdfService, PdfDownloadButtonComponent } from '@hemo/pdf-client';

this.pdfService.generateAndOpen({
  reportTemplateId: 'template-01-dialysis-session',
  tenantCode: 'tenant-demo-a',
  entityId: sessionId,
  data: reportDto,
});
```

Copy sources from `client/projects/hemo-pdf-client/src` into Hemo-frontend or link via path.

## Report Preview (`@hemo/report-viewer`)

```typescript
import { HEMO_REPORT_VIEWER_CONFIG, HemoReportPreviewService, HemoReportViewerComponent } from '@hemo/report-viewer';

providers: [
  {
    provide: HEMO_REPORT_VIEWER_CONFIG,
    useValue: {
      pdfApiUrl: 'http://localhost:5090',
      getAuthToken: () => localStorage.getItem('token'),
      getTenantCode: () => currentTenantCode,
    },
  },
]
```

```typescript
this.previewService.load({
  reportTemplateId: 'template-02-lab-result',
  tenantCode: 'tenant-demo-a',
  entityId: 'test-1',
  data: { patientName: 'Test' },
}).subscribe((doc) => { this.document = doc; });
```

```html
<hemo-report-viewer [document]="document" (print)="onPrint()" (download)="onDownload()" />
```

Print/Download ยังใช้ `@hemo/pdf-client` → `POST /api/pdf/generate`

## Configuration (`appsettings.json`)

| Key | Description |
|-----|-------------|
| `HemoPdf:UseMockServices` | `true` = mock auth + signatures |
| `HemoPdf:BrandingRootPath` | Path to `assets/branding` |
| `HemoPdf:Jwt:Authority` | JWT authority (production) |
| `HemoPdf:CorsOrigins` | Allowed Angular origins |

## Branding

เพิ่มไฟล์ `assets/branding/{tenantCode}.json` — ดูตัวอย่าง `tenant-demo-a.json`  
อนาคต: `DbBrandingStore` — ดู [docs/BRANDING-GUIDELINE.md](./docs/BRANDING-GUIDELINE.md)

## Docs

- [.cursor/docs/PDF-REPORT-SYSTEM.md](./.cursor/docs/PDF-REPORT-SYSTEM.md) — สรุประบบ PDF/Preview ทั้ง 3 repo (สถานะปัจจุบัน + flow + fallback + วิธีขึ้น template ใหม่)
- [01-IMPLEMENT-PLANNING.md](./01-IMPLEMENT-PLANNING.md) — แผนออกแบบโมดูล + decision log
- [02-FEATURE-PREVIEW-PDF.md](./02-FEATURE-PREVIEW-PDF.md) — Report Preview (`@hemo/report-viewer`, แทน Telerik)
- [03-IMPLEMENT-REPORT-LAYOUT.md](./03-IMPLEMENT-REPORT-LAYOUT.md) — แผน Hemosheet layout parity

## Related Repos

| Repo | Role |
|------|------|
| Hemo-backend | ส่ง DTO + permission (ไม่ reference QuestPDF) |
| Hemo-frontend | ใช้ `@hemo/pdf-client` |
| NSS | แพทเทิร์นอ้างอิง QuestPDF |
