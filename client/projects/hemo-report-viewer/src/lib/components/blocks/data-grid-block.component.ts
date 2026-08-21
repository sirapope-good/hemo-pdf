import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataGridReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-data-grid-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <table>
        <colgroup>
          <col *ngFor="let weight of resolvedWeights" [style.width.%]="columnWidthPercent(weight)" />
        </colgroup>
        <thead>
          <tr *ngIf="block.title" class="hemo-section-title-row">
            <th [attr.colspan]="block.columns.length">{{ block.title }}</th>
          </tr>
          <tr>
            <th *ngFor="let column of block.columns" scope="col">{{ column }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows" [class.hemo-data-grid__section-row]="isSectionRow(row)">
            <td *ngFor="let cell of row; let i = index" [class.hemo-data-grid__note-cell]="isNoteColumn(i)">
              {{ formatCell(cell) }}
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class DataGridBlockComponent {
  @Input({ required: true }) block!: DataGridReportBlock;

  get resolvedWeights(): number[] {
    if (this.block.columnWeights?.length === this.block.columns.length) {
      return this.block.columnWeights;
    }

    return this.block.columns.map(() => 1);
  }

  columnWidthPercent(weight: number): number {
    const total = this.resolvedWeights.reduce((sum, value) => sum + value, 0);
    return total > 0 ? (weight / total) * 100 : 100 / this.block.columns.length;
  }

  isNoteColumn(index: number): boolean {
    return this.block.columns[index]?.includes('หมายเหตุ') ?? false;
  }

  isSectionRow(row: (string | boolean)[]): boolean {
    if (row.length < 2) {
      return false;
    }

    const first = this.formatCell(row[0]).trim();
    if (!first) {
      return false;
    }

    return row.slice(1).every(cell => !this.formatCell(cell).trim());
  }

  formatCell(cell: string | boolean): string {
    if (typeof cell === 'boolean') {
      return cell ? '✓' : '';
    }
    return cell;
  }
}
