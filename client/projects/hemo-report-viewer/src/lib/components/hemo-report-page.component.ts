import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportDocument } from '../models/report-document.model';
import { HemoReportHeaderComponent } from './hemo-report-header.component';
import { HemoReportFooterComponent } from './hemo-report-footer.component';
import { HemoReportBlockOutletComponent } from './hemo-report-block-outlet.component';

@Component({
  selector: 'hemo-report-page',
  standalone: true,
  imports: [
    CommonModule,
    HemoReportHeaderComponent,
    HemoReportFooterComponent,
    HemoReportBlockOutletComponent,
  ],
  template: `
    <article class="hemo-report-page">
      <header class="hemo-report-page__header-band">
        <hemo-report-header [branding]="document.branding" [header]="document.header" />
      </header>

      <div class="hemo-report-page__content">
        @for (block of pageBlocks; track $index) {
          <hemo-report-block-outlet [block]="block" />
        }
      </div>

      <footer class="hemo-report-page__footer-band">
        <hemo-report-footer [footer]="document.footer" />
      </footer>
    </article>
  `,
})
export class HemoReportPageComponent {
  @Input({ required: true }) document!: ReportDocument;
  @Input() pageIndex = 0;

  get pageBlocks() {
    return this.document.pages[this.pageIndex]?.blocks ?? [];
  }
}
