import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBlock, SectionRowReportBlock } from '../../models/report-document.model';
import { HemoReportBlockOutletComponent } from '../hemo-report-block-outlet.component';

@Component({
  selector: 'hemo-section-row-block',
  standalone: true,
  imports: [CommonModule, forwardRef(() => HemoReportBlockOutletComponent)],
  template: `
    <section class="hemo-report-block hemo-section-row" [style.--hemo-section-columns]="block.columns">
      <div class="hemo-section-row__grid">
        @for (child of block.blocks; track $index) {
          <div class="hemo-section-row__column">
            @if (isColumnStack(child)) {
              @for (nested of child.blocks; track $index) {
                <hemo-report-block-outlet [block]="nested" />
              }
            } @else {
              <hemo-report-block-outlet [block]="child" />
            }
          </div>
        }
      </div>
    </section>
  `,
})
export class SectionRowBlockComponent {
  @Input({ required: true }) block!: SectionRowReportBlock;

  isColumnStack(block: unknown): block is { type: 'column-stack'; blocks: ReportBlock[] } {
    return (block as { type?: string }).type === 'column-stack';
  }
}
