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
  | FieldGridReportBlock
  | DataGridReportBlock
  | ChecklistTableReportBlock
  | VascularAccessReportBlock
  | SignatureReportBlock
  | TextReportBlock
  | SubHeaderBarReportBlock
  | ColumnStackReportBlock
  | SectionRowReportBlock
  | ChecklistClusterReportBlock
  | PrePostHdNotesReportBlock
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

export interface FieldGridReportBlock {
  type: 'field-grid';
  title?: string;
  columns: number;
  fields: FieldGridField[];
}

export interface FieldGridField {
  label: string;
  value: string;
  columnSpan?: number;
}

export interface DataGridReportBlock {
  type: 'data-grid';
  title?: string;
  columns: string[];
  columnWeights?: number[];
  rows: (string | boolean)[][];
}

export interface ChecklistTableReportBlock {
  type: 'checklist-table';
  title?: string;
  layout?: 'default' | 'yn-columns' | string;
  columns: string[];
  rows: ChecklistCellValue[][];
}

export type ChecklistCellValue = ChecklistTextCell | ChecklistCheckboxCell;

export interface ChecklistTextCell {
  kind: 'text';
  text: string;
}

export interface ChecklistCheckboxCell {
  kind: 'checkbox';
  checked: boolean;
  label?: string;
}

export interface VascularAccessReportBlock {
  type: 'vascular-access';
  title?: string;
  variant: 'av-fistula' | 'perm-cath' | string;
  rows: LabelValue[];
}

export interface SignatureReportBlock {
  type: 'signature';
  slots: SignatureSlot[];
}

export interface TextReportBlock {
  type: 'text';
  title?: string;
  content: string;
  style?: 'title' | 'body' | 'caption' | string;
}

export interface SubHeaderBarReportBlock {
  type: 'sub-header-bar';
  fields: LabelValue[];
}

export interface ColumnStackReportBlock {
  type: 'column-stack';
  blocks: ReportBlock[];
}

export interface SectionRowReportBlock {
  type: 'section-row';
  columns: number;
  blocks: ReportBlock[];
}

export interface ChecklistClusterReportBlock {
  type: 'checklist-cluster';
  tables: ChecklistTableReportBlock[];
}

export interface PrePostHdNotesReportBlock {
  type: 'pre-post-hd-notes';
  preHdContent?: string;
  preHdSigner?: string;
  postHdContent?: string;
  postHdSigner?: string;
}
