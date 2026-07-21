import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBranding, ReportHeaderBlock } from '../models/report-document.model';

@Component({
  selector: 'hemo-report-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="hemo-report-header">
      <div class="hemo-report-header__band">
        <div class="hemo-report-header__brand">
          <img *ngIf="branding.logoUrl" [src]="branding.logoUrl" alt="" class="hemo-report-header__logo" />
          <div *ngFor="let line of branding.companyLines" class="hemo-report-header__company-line">
            {{ line }}
          </div>
        </div>

        <div class="hemo-report-header__title-block">
          <h2 *ngIf="header.title" class="hemo-report-header__title">{{ header.title }}</h2>
          <div *ngIf="header.reportCode" class="hemo-report-header__code">{{ header.reportCode }}</div>
        </div>

        <div class="hemo-report-header__meta">
          <div *ngFor="let line of header.metadataLines" class="hemo-report-header__meta-line">
            {{ line }}
          </div>
        </div>
      </div>
    </header>
  `,
})
export class HemoReportHeaderComponent {
  @Input({ required: true }) branding!: ReportBranding;
  @Input({ required: true }) header!: ReportHeaderBlock;
}
