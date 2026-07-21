import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBlock } from '../models/report-document.model';
import { PatientInfoBlockComponent } from './blocks/patient-info-block.component';
import { FieldGridBlockComponent } from './blocks/field-grid-block.component';
import { KeyValueTableBlockComponent } from './blocks/key-value-table-block.component';
import { DataGridBlockComponent } from './blocks/data-grid-block.component';
import { ChecklistTableBlockComponent } from './blocks/checklist-table-block.component';
import { VascularAccessBlockComponent } from './blocks/vascular-access-block.component';
import { SignatureBlockComponent } from './blocks/signature-block.component';
import { SubHeaderBarBlockComponent } from './blocks/sub-header-bar-block.component';
import { SectionRowBlockComponent } from './blocks/section-row-block.component';
import { ChecklistClusterBlockComponent } from './blocks/checklist-cluster-block.component';
import { PrePostHdNotesBlockComponent } from './blocks/pre-post-hd-notes-block.component';
import { ColumnStackBlockComponent } from './blocks/column-stack-block.component';

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
    SubHeaderBarBlockComponent,
    SectionRowBlockComponent,
    ChecklistClusterBlockComponent,
    PrePostHdNotesBlockComponent,
    forwardRef(() => ColumnStackBlockComponent),
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
      @case ('sub-header-bar') {
        <hemo-sub-header-bar-block [block]="$any(block)" />
      }
      @case ('section-row') {
        <hemo-section-row-block [block]="$any(block)" />
      }
      @case ('column-stack') {
        <hemo-column-stack-block [block]="$any(block)" />
      }
      @case ('checklist-cluster') {
        <hemo-checklist-cluster-block [block]="$any(block)" />
      }
      @case ('pre-post-hd-notes') {
        <hemo-pre-post-hd-notes-block [block]="$any(block)" />
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
      @default {
        <section class="hemo-report-block hemo-report-block--unknown" role="note">
          <p class="hemo-report-block__caption">Unsupported block type: {{ block.type }}</p>
        </section>
      }
    }
  `,
})
export class HemoReportBlockOutletComponent {
  @Input({ required: true }) block!: ReportBlock;
}
