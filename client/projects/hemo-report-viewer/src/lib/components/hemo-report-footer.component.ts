import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportFooterBlock } from '../models/report-document.model';

@Component({
  selector: 'hemo-report-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="hemo-report-footer">
      <div *ngIf="footer.signatures?.length" class="hemo-report-footer__signatures">
        <div *ngFor="let sig of footer.signatures" class="hemo-report-footer__signature-slot">
          <div class="hemo-report-footer__signature-role">{{ sig.role }}</div>
          <img *ngIf="sig.imageUrl" [src]="sig.imageUrl" alt="" class="hemo-report-footer__signature-image" />
          <div *ngIf="sig.name" class="hemo-report-footer__signature-name">{{ sig.name }}</div>
        </div>
      </div>

      <div class="hemo-report-footer__bottom">
        <div class="hemo-report-footer__lines">
          <div *ngFor="let line of footer.lines">{{ line }}</div>
        </div>
        <div *ngIf="footer.pageNumber" class="hemo-report-footer__page">
          หน้า {{ footer.pageNumber.current }} / {{ footer.pageNumber.total }}
        </div>
      </div>
    </footer>
  `,
})
export class HemoReportFooterComponent {
  @Input({ required: true }) footer!: ReportFooterBlock;
}
