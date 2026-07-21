import { Injectable, Inject, Optional } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneratePdfRequest, HemoPdfConfig } from '../models/preview-request.model';
import { ReportDocument } from '../models/report-document.model';
import { HEMO_REPORT_VIEWER_CONFIG } from '../tokens/hemo-report-viewer-config.token';

@Injectable({ providedIn: 'root' })
export class HemoReportPreviewService {
  constructor(
    private readonly http: HttpClient,
    @Optional() @Inject(HEMO_REPORT_VIEWER_CONFIG) private readonly config: HemoPdfConfig | null
  ) {}

  load(request: GeneratePdfRequest): Observable<ReportDocument> {
    const url = `${this.resolveBaseUrl()}/api/report/preview`;
    const headers = this.buildHeaders(request.tenantCode);

    return this.http.post<ReportDocument>(url, request, { headers });
  }

  private resolveBaseUrl(): string {
    const base = this.config?.pdfApiUrl?.replace(/\/$/, '');
    if (!base) {
      throw new Error(
        'HemoReportPreviewService: pdfApiUrl is not configured. Provide HEMO_REPORT_VIEWER_CONFIG.'
      );
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
