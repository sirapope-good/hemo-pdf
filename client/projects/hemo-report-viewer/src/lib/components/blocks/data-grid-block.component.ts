import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataGridReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-data-grid-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <h3 *ngIf="block.title" class="hemo-report-block__title">{{ block.title }}</h3>
      <table>
        <thead>
          <tr>
            <th *ngFor="let column of block.columns" scope="col">{{ column }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <td *ngFor="let cell of row">{{ formatCell(cell) }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class DataGridBlockComponent {
  @Input({ required: true }) block!: DataGridReportBlock;

  formatCell(cell: string | boolean): string {
    if (typeof cell === 'boolean') {
      return cell ? '✓' : '';
    }
    return cell;
  }
}
