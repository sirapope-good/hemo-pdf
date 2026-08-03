import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChecklistTableReportBlock, ChecklistCheckboxCell, ChecklistTextCell } from '../../models/report-document.model';

@Component({
  selector: 'hemo-checklist-table-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <table
        class="hemo-checklist-table"
        [class.hemo-checklist-table--yn]="block.layout === 'yn-columns'"
        [class.hemo-checklist-table--pre-re]="block.layout === 'pre-re-matrix'"
      >
        <thead>
          <tr *ngIf="block.title" class="hemo-section-title-row">
            <th [attr.colspan]="block.columns.length">{{ block.title }}</th>
          </tr>
          <tr>
            <th
              *ngFor="let col of block.columns; let i = index"
              [class.hemo-checklist-table__checkbox-col]="isCheckboxColumn(i)"
            >
              {{ col }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <td
              *ngFor="let cell of row; let i = index"
              [class.hemo-checklist-table__checkbox-col]="isCheckboxColumn(i)"
            >
              <ng-container [ngSwitch]="cell.kind">
                <span
                  *ngSwitchCase="'checkbox'"
                  class="hemo-checklist-table__box"
                  [class.hemo-checklist-table__box--checked]="asCheckbox(cell).checked"
                >
                  {{ asCheckbox(cell).checked ? '/' : '' }}
                </span>
                <span *ngSwitchDefault>{{ asText(cell).text }}</span>
              </ng-container>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class ChecklistTableBlockComponent {
  @Input({ required: true }) block!: ChecklistTableReportBlock;

  isCheckboxColumn(index: number): boolean {
    if (this.block.layout === 'yn-columns') {
      return index < 2;
    }

    if (this.block.layout === 'pre-re-matrix') {
      return index >= 1 && index <= 4;
    }

    return index === 0;
  }

  asCheckbox(cell: unknown): ChecklistCheckboxCell {
    return cell as ChecklistCheckboxCell;
  }

  asText(cell: unknown): ChecklistTextCell {
    return cell as ChecklistTextCell;
  }
}
