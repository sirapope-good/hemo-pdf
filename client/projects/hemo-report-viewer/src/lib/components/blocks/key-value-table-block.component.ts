import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KeyValueTableReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-key-value-table-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <table>
        <thead *ngIf="block.title">
          <tr class="hemo-section-title-row">
            <th colspan="2">{{ block.title }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <th scope="row">{{ row.label }}</th>
            <td>{{ row.value || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class KeyValueTableBlockComponent {
  @Input({ required: true }) block!: KeyValueTableReportBlock;
}
