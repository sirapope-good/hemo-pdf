import { Injectable, Inject, Optional } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { GeneratePdfRequest, HemoPdfConfig } from '../models/generate-pdf-request.model';
import { HEMO_PDF_CONFIG } from '../tokens/hemo-pdf-config.token';

@Injectable({ providedIn: 'root' })
export class HemoPdfService {
  constructor(
    private readonly http: HttpClient,
    @Optional() @Inject(HEMO_PDF_CONFIG) private readonly config: HemoPdfConfig | null
  ) {}

  generateBlob(request: GeneratePdfRequest): Observable<Blob> {
    const url = `${this.resolveBaseUrl()}/api/pdf/generate`;
    const headers = this.buildHeaders(request.tenantCode);

    return this.http.post(url, request, {
      responseType: 'blob',
      headers,
    });
  }

  generateAndOpen(request: GeneratePdfRequest): Observable<void> {
    return this.generateBlob(request).pipe(
      tap((blob) => {
        const blobUrl = URL.createObjectURL(blob);
        window.open(blobUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(blobUrl), 100);
      }),
      map(() => void 0)
    );
  }

  download(request: GeneratePdfRequest, fileName?: string): Observable<void> {
    return this.generateBlob(request).pipe(
      tap((blob) => {
        const blobUrl = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = blobUrl;
        anchor.download = fileName ?? `report-${request.entityId ?? 'export'}.pdf`;
        document.body.appendChild(anchor);
        anchor.click();
        URL.revokeObjectURL(blobUrl);
        document.body.removeChild(anchor);
      }),
      map(() => void 0)
    );
  }

  private resolveBaseUrl(): string {
    const base = this.config?.pdfApiUrl?.replace(/\/$/, '');
    if (!base) {
      throw new Error('HemoPdfService: pdfApiUrl is not configured. Provide HEMO_PDF_CONFIG.');
    }
    return base;
  }

  private buildHeaders(tenantCode: string): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    const token = this.config?.getAuthToken?.();
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    const tenant = this.config?.getTenantCode?.() ?? tenantCode;
    if (tenant) {
      headers = headers.set('X-Tenant-Code', tenant);
    }

    return headers;
  }
}
