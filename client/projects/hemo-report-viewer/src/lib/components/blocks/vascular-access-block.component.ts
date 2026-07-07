import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VascularAccessReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-vascular-access-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block hemo-vascular-access-block">
      <h3 *ngIf="block.title" class="hemo-report-block__title">{{ block.title }}</h3>
      <table>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <th scope="row">{{ row.label }}</th>
            <td>{{ row.value }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class VascularAccessBlockComponent {
  @Input({ required: true }) block!: VascularAccessReportBlock;
}
