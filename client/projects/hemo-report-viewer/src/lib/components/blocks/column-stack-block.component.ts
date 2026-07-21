import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ColumnStackReportBlock } from '../../models/report-document.model';
import { HemoReportBlockOutletComponent } from '../hemo-report-block-outlet.component';

@Component({
  selector: 'hemo-column-stack-block',
  standalone: true,
  imports: [CommonModule, forwardRef(() => HemoReportBlockOutletComponent)],
  template: `
    <section class="hemo-report-block hemo-column-stack">
      @for (child of block.blocks; track $index) {
        <hemo-report-block-outlet [block]="child" />
      }
    </section>
  `,
})
export class ColumnStackBlockComponent {
  @Input({ required: true }) block!: ColumnStackReportBlock;
}
