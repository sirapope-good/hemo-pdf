# @hemo/report-viewer

Angular HTML/CSS viewer for **Hemo-PDF** `ReportDocument` JSON.

## Setup

```typescript
import { HEMO_REPORT_VIEWER_CONFIG } from '@hemo/report-viewer';

providers: [
  {
    provide: HEMO_REPORT_VIEWER_CONFIG,
    useValue: {
      pdfApiUrl: 'http://localhost:5090',
      getAuthToken: () => token,
      getTenantCode: () => tenantCode,
    },
  },
]
```

## Usage

```typescript
import { HemoReportPreviewService, HemoReportViewerComponent } from '@hemo/report-viewer';

this.previewService.load(request).subscribe((doc) => {
  this.document = doc;
});
```

```html
<hemo-report-viewer
  [document]="document"
  [loading]="loading"
  (print)="onPrint()"
  (download)="onDownload()" />
```

Print/Download should call `@hemo/pdf-client` → `POST /api/pdf/generate`.
