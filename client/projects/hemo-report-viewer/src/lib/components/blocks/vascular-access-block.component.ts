import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VascularAccessReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-vascular-access-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block hemo-vascular-access-block">
      <table>
        <thead *ngIf="block.title">
          <tr class="hemo-section-title-row">
            <th colspan="2">{{ block.title }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <th scope="row">{{ row.label }}</th>
            <td>{{ displayValue(row.value) }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class VascularAccessBlockComponent {
  @Input({ required: true }) block!: VascularAccessReportBlock;

  displayValue(value: string | null | undefined): string {
    return value?.trim() ? value : '—';
  }
}
