import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChecklistClusterReportBlock } from '../../models/report-document.model';
import { ChecklistTableBlockComponent } from './checklist-table-block.component';

@Component({
  selector: 'hemo-checklist-cluster-block',
  standalone: true,
  imports: [CommonModule, ChecklistTableBlockComponent],
  template: `
    <section class="hemo-report-block hemo-checklist-cluster">
      <div class="hemo-checklist-cluster__grid">
        @for (table of block.tables; track $index) {
          <hemo-checklist-table-block [block]="table" />
        }
      </div>
    </section>
  `,
})
export class ChecklistClusterBlockComponent {
  @Input({ required: true }) block!: ChecklistClusterReportBlock;
}
