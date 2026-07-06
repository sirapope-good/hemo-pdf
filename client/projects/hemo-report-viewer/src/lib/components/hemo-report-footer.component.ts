import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportFooterBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-report-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="hemo-report-footer">
      <div>
        <div *ngFor="let line of footer.lines">{{ line }}</div>
        <div *ngFor="let sig of footer.signatures">
          {{ sig.role }}: {{ sig.name || '—' }}
        </div>
      </div>
      <div *ngIf="footer.pageNumber">
        หน้า {{ footer.pageNumber.current }} / {{ footer.pageNumber.total }}
      </div>
    </footer>
  `,
})
export class HemoReportFooterComponent {
  @Input({ required: true }) footer!: ReportFooterBlock;
}
