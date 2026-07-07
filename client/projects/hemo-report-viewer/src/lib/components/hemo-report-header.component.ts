import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBranding, ReportHeaderBlock } from '../models/report-document.model';

@Component({
  selector: 'hemo-report-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header
      class="hemo-report-header"
      [class.hemo-report-header--left]="branding.alignment === 'left'"
      [class.hemo-report-header--right]="branding.alignment === 'right'">
      <img *ngIf="branding.logoUrl" [src]="branding.logoUrl" alt="" class="hemo-report-header__logo" />
      <div *ngFor="let line of branding.companyLines" class="hemo-report-header__company-line">
        {{ line }}
      </div>
      <h2 *ngIf="header.title" class="hemo-report-header__title">{{ header.title }}</h2>
      <div *ngIf="header.subtitle" class="hemo-report-header__subtitle">{{ header.subtitle }}</div>
      <div *ngIf="header.reportCode" class="hemo-report-header__code">{{ header.reportCode }}</div>
    </header>
  `,
})
export class HemoReportHeaderComponent {
  @Input({ required: true }) branding!: ReportBranding;
  @Input({ required: true }) header!: ReportHeaderBlock;
}
