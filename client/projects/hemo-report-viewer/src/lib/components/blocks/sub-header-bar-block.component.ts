import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubHeaderBarReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-sub-header-bar-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block hemo-sub-header-bar">
      <table class="hemo-sub-header-bar__table">
        <tbody>
          <tr>
            <td *ngFor="let field of block.fields">
              <span class="hemo-label">{{ field.label }}:</span>
              <span class="hemo-value">{{ field.value || '—' }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class SubHeaderBarBlockComponent {
  @Input({ required: true }) block!: SubHeaderBarReportBlock;
}
