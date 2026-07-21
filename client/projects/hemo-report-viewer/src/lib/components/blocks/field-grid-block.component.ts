import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FieldGridReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-field-grid-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <div
        class="hemo-field-grid"
        [style.gridTemplateColumns]="'repeat(' + block.columns + ', 1fr)'">
        <div *ngIf="block.title" class="hemo-field-grid__title-row">{{ block.title }}</div>
        <div
          *ngFor="let field of block.fields"
          class="hemo-field-grid__cell"
          [style.gridColumn]="'span ' + (field.columnSpan || 1)">
          <span class="hemo-field-grid__label">{{ field.label }}:</span>
          <span class="hemo-field-grid__value">{{ field.value || '—' }}</span>
        </div>
      </div>
    </section>
  `,
})
export class FieldGridBlockComponent {
  @Input({ required: true }) block!: FieldGridReportBlock;
}
