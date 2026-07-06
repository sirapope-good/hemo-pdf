# @hemo/pdf-client

Angular library สำหรับเรียก Hemo-PDF Standalone API

## Usage

1. Copy `src/lib` ไปยัง Hemo-frontend หรือใช้ path dependency
2. Provide `HEMO_PDF_CONFIG` with `pdfApiUrl`
3. Import `PdfDownloadButtonComponent` (standalone) หรือใช้ `HemoPdfService`

## API

- `HemoPdfService.generateBlob(request)` → `Observable<Blob>`
- `HemoPdfService.generateAndOpen(request)` → เปิด PDF ในแท็บใหม่
- `HemoPdfService.download(request, fileName?)` → ดาวน์โหลด

## Headers sent

- `Authorization: Bearer {token}`
- `X-Tenant-Code: {tenantCode}`
- `Content-Type: application/json`
