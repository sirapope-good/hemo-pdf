export interface ReportDocument {
  meta: ReportDocumentMeta;
  branding: ReportBranding;
  header: ReportHeaderBlock;
  pages: ReportPage[];
  footer: ReportFooterBlock;
}

export interface ReportDocumentMeta {
  templateId: string;
  title: string;
  pageSize: 'A4' | string;
  generatedAt?: string;
}

export interface ReportBranding {
  logoUrl?: string;
  companyLines: string[];
  alignment: 'left' | 'center' | 'right' | string;
}

export interface ReportHeaderBlock {
  title?: string;
  subtitle?: string;
  reportCode?: string;
  metadataLines?: string[];
}

export interface ReportFooterBlock {
  type: 'page-number' | 'signed' | 'configurable' | string;
  lines?: string[];
  pageNumber?: { current: number; total: number };
  signatures?: SignatureSlot[];
}

export interface ReportPage {
  blocks: ReportBlock[];
}

export interface LabelValue {
  label: string;
  value: string;
}

export interface SignatureSlot {
  role: string;
  name?: string;
  signedAt?: string;
  imageUrl?: string;
}

export type ReportBlock =
  | PatientInfoReportBlock
  | KeyValueTableReportBlock
  | DataGridReportBlock
  | SignatureReportBlock
  | TextReportBlock
  | { type: string; [key: string]: unknown };

export interface PatientInfoReportBlock {
  type: 'patient-info';
  title?: string;
  columns: LabelValue[][];
}

export interface KeyValueTableReportBlock {
  type: 'key-value-table';
  title?: string;
  rows: LabelValue[];
}

export interface DataGridReportBlock {
  type: 'data-grid';
  title?: string;
  columns: string[];
  rows: (string | boolean)[][];
}

export interface SignatureReportBlock {
  type: 'signature';
  slots: SignatureSlot[];
}

export interface TextReportBlock {
  type: 'text';
  content: string;
  style?: 'title' | 'body' | 'caption' | string;
}
