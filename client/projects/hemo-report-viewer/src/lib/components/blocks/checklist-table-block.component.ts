import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChecklistTableReportBlock, ChecklistCheckboxCell, ChecklistTextCell } from '../../models/report-document.model';

@Component({
  selector: 'hemo-checklist-table-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <table class="hemo-checklist-table">
        <thead>
          <tr>
            <th *ngFor="let col of block.columns">{{ col }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of block.rows">
            <td *ngFor="let cell of row">
              <ng-container [ngSwitch]="cell.kind">
                <label *ngSwitchCase="'checkbox'" class="hemo-checklist-table__checkbox">
                  <input type="checkbox" [checked]="asCheckbox(cell).checked" disabled />
                  <span>{{ asCheckbox(cell).label }}</span>
                </label>
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

  asCheckbox(cell: unknown): ChecklistCheckboxCell {
    return cell as ChecklistCheckboxCell;
  }

  asText(cell: unknown): ChecklistTextCell {
    return cell as ChecklistTextCell;
  }
}
