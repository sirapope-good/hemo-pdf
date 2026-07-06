import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportDocument, ReportBlock, PatientInfoReportBlock, KeyValueTableReportBlock, DataGridReportBlock, SignatureReportBlock, TextReportBlock } from '../models/report-document.model';
import { HemoReportHeaderComponent } from './hemo-report-header.component';
import { HemoReportFooterComponent } from './hemo-report-footer.component';
import { KeyValueTableBlockComponent } from './blocks/key-value-table-block.component';
import { PatientInfoBlockComponent } from './blocks/patient-info-block.component';
import { DataGridBlockComponent } from './blocks/data-grid-block.component';
import { SignatureBlockComponent } from './blocks/signature-block.component';

@Component({
  selector: 'hemo-report-page',
  standalone: true,
  imports: [
    CommonModule,
    HemoReportHeaderComponent,
    HemoReportFooterComponent,
    KeyValueTableBlockComponent,
    PatientInfoBlockComponent,
    DataGridBlockComponent,
    SignatureBlockComponent,
  ],
  template: `
    <article class="hemo-report-page">
      <hemo-report-header [branding]="document.branding" [header]="document.header" />
      <ng-container *ngFor="let block of pageBlocks">
        <hemo-patient-info-block *ngIf="block.type === 'patient-info'" [block]="asPatientInfo(block)" />
        <hemo-key-value-table-block *ngIf="block.type === 'key-value-table'" [block]="asKeyValue(block)" />
        <hemo-data-grid-block *ngIf="block.type === 'data-grid'" [block]="asDataGrid(block)" />
        <hemo-signature-block *ngIf="block.type === 'signature'" [block]="asSignature(block)" />
        <section *ngIf="block.type === 'text'" class="hemo-report-block">
          <p>{{ asText(block).content }}</p>
        </section>
      </ng-container>
      <hemo-report-footer [footer]="document.footer" />
    </article>
  `,
})
export class HemoReportPageComponent {
  @Input({ required: true }) document!: ReportDocument;
  @Input() pageIndex = 0;

  get pageBlocks(): ReportBlock[] {
    return this.document.pages[this.pageIndex]?.blocks ?? [];
  }

  asPatientInfo(block: ReportBlock): PatientInfoReportBlock {
    return block as PatientInfoReportBlock;
  }

  asKeyValue(block: ReportBlock): KeyValueTableReportBlock {
    return block as KeyValueTableReportBlock;
  }

  asDataGrid(block: ReportBlock): DataGridReportBlock {
    return block as DataGridReportBlock;
  }

  asSignature(block: ReportBlock): SignatureReportBlock {
    return block as SignatureReportBlock;
  }

  asText(block: ReportBlock): TextReportBlock {
    return block as TextReportBlock;
  }
}
