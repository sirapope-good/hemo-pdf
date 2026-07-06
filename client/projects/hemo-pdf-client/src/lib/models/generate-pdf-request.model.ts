export interface GeneratePdfRequest {
  reportTemplateId: string;
  tenantCode: string;
  entityId?: string;
  data: Record<string, unknown>;
  signatures?: ReportSignatureContext;
}

export interface SignatureInfo {
  signerName: string;
  signerRole?: string;
  signedAt?: string;
}

export interface ReportSignatureContext {
  isFullySigned: boolean;
  signatures: SignatureInfo[];
}

export interface HemoPdfConfig {
  pdfApiUrl: string;
  getAuthToken?: () => string | null | undefined;
  getTenantCode?: () => string | null | undefined;
}
