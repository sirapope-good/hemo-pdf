import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'hemo-report-toolbar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="hemo-report-viewer__toolbar">
      <button type="button" (click)="zoomOut.emit()" [disabled]="busy || scale <= minScale">−</button>
      <span>{{ scalePercent }}%</span>
      <button type="button" (click)="zoomIn.emit()" [disabled]="busy || scale >= maxScale">+</button>
      <span>|</span>
      <button type="button" (click)="prevPage.emit()" [disabled]="busy || pageIndex <= 0">‹</button>
      <span>{{ pageIndex + 1 }} / {{ pageCount }}</span>
      <button type="button" (click)="nextPage.emit()" [disabled]="busy || pageIndex >= pageCount - 1">›</button>
      <span>|</span>
      <button type="button" class="hemo-report-viewer__action-btn" (click)="print.emit()" [disabled]="busy">
        <span *ngIf="printing" class="hemo-report-viewer__btn-spinner" aria-hidden="true"></span>
        {{ printing ? 'กำลังพิมพ์…' : 'Print' }}
      </button>
      <button type="button" class="hemo-report-viewer__action-btn" (click)="download.emit()" [disabled]="busy">
        <span *ngIf="downloading" class="hemo-report-viewer__btn-spinner" aria-hidden="true"></span>
        {{ downloading ? 'กำลังดาวน์โหลด…' : 'Download' }}
      </button>
    </div>
  `,
})
export class HemoReportToolbarComponent {
  @Input() scale = 1;
  @Input() pageIndex = 0;
  @Input() pageCount = 1;
  @Input() minScale = 0.5;
  @Input() maxScale = 2;
  @Input() printing = false;
  @Input() downloading = false;

  @Output() zoomIn = new EventEmitter<void>();
  @Output() zoomOut = new EventEmitter<void>();
  @Output() prevPage = new EventEmitter<void>();
  @Output() nextPage = new EventEmitter<void>();
  @Output() print = new EventEmitter<void>();
  @Output() download = new EventEmitter<void>();

  get scalePercent(): number {
    return Math.round(this.scale * 100);
  }

  get busy(): boolean {
    return this.printing || this.downloading;
  }
}
