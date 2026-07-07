import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBlock } from '../models/report-document.model';
import { PatientInfoBlockComponent } from './blocks/patient-info-block.component';
import { FieldGridBlockComponent } from './blocks/field-grid-block.component';
import { KeyValueTableBlockComponent } from './blocks/key-value-table-block.component';
import { DataGridBlockComponent } from './blocks/data-grid-block.component';
import { ChecklistTableBlockComponent } from './blocks/checklist-table-block.component';
import { VascularAccessBlockComponent } from './blocks/vascular-access-block.component';
import { SignatureBlockComponent } from './blocks/signature-block.component';

@Component({
  selector: 'hemo-report-block-outlet',
  standalone: true,
  imports: [
    CommonModule,
    PatientInfoBlockComponent,
    FieldGridBlockComponent,
    KeyValueTableBlockComponent,
    DataGridBlockComponent,
    ChecklistTableBlockComponent,
    VascularAccessBlockComponent,
    SignatureBlockComponent,
  ],
  template: `
    @switch (block.type) {
      @case ('patient-info') {
        <hemo-patient-info-block [block]="$any(block)" />
      }
      @case ('field-grid') {
        <hemo-field-grid-block [block]="$any(block)" />
      }
      @case ('key-value-table') {
        <hemo-key-value-table-block [block]="$any(block)" />
      }
      @case ('data-grid') {
        <hemo-data-grid-block [block]="$any(block)" />
      }
      @case ('checklist-table') {
        <hemo-checklist-table-block [block]="$any(block)" />
      }
      @case ('vascular-access') {
        <hemo-vascular-access-block [block]="$any(block)" />
      }
      @case ('signature') {
        <hemo-signature-block [block]="$any(block)" />
      }
      @case ('text') {
        <section class="hemo-report-block">
          @if ($any(block).title) {
            <h3 class="hemo-report-block__title">{{ $any(block).title }}</h3>
          }
          <p [class.hemo-report-block__caption]="$any(block).style === 'caption'">
            {{ $any(block).content }}
          </p>
        </section>
      }
    }
  `,
})
export class HemoReportBlockOutletComponent {
  @Input({ required: true }) block!: ReportBlock;
}
