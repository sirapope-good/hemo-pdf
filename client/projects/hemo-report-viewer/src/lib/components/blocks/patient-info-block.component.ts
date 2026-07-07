import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PatientInfoReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-patient-info-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block hemo-patient-info-block">
      <div class="hemo-patient-info__panel">
        <div *ngIf="block.title" class="hemo-patient-info__title-row">{{ block.title }}</div>
        <div class="hemo-patient-info__columns">
          <div *ngFor="let column of block.columns" class="hemo-patient-info__column">
            <div *ngFor="let field of column" class="hemo-patient-info__row">
              <span class="hemo-patient-info__label">{{ field.label }}:</span>
              <span>{{ field.value || '—' }}</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
})
export class PatientInfoBlockComponent {
  @Input({ required: true }) block!: PatientInfoReportBlock;
}
