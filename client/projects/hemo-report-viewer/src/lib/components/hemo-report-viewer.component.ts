import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild,
  ViewEncapsulation,
  afterNextRender,
  inject,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HemoReportToolbarComponent } from './hemo-report-toolbar.component';
import { HemoReportPdfCanvasComponent } from './hemo-report-pdf-canvas.component';

@Component({
  selector: 'hemo-report-viewer',
  standalone: true,
  imports: [CommonModule, HemoReportToolbarComponent, HemoReportPdfCanvasComponent],
  encapsulation: ViewEncapsulation.None,
  styleUrls: ['../styles/report-viewer.scss'],
  template: `
    <div class="hemo-report-viewer">
      <hemo-report-toolbar
        [scale]="scale()"
        [pageIndex]="pageIndex()"
        [pageCount]="pageCount()"
        [printing]="printing"
        [downloading]="downloading"
        (zoomIn)="zoomIn()"
        (zoomOut)="zoomOut()"
        (prevPage)="prevPage()"
        (nextPage)="nextPage()"
        (print)="print.emit()"
        (download)="download.emit()" />

      <div #canvasHost class="hemo-report-viewer__canvas">
        <hemo-report-pdf-canvas
          *ngIf="pdfBlob"
          [pdfBlob]="pdfBlob"
          [pageIndex]="pageIndex()"
          [scale]="scale()"
          [workerSrc]="workerSrc"
          (pageCountChange)="onPageCountChange($event)"
          (pageWidthChange)="onPageWidthChange($event)" />
        <div *ngIf="!pdfBlob && loading" class="hemo-report-viewer__loading">
          <span class="hemo-report-viewer__spinner" aria-hidden="true"></span>
          <span>กำลังโหลดตัวอย่างรายงาน…</span>
        </div>
        <div *ngIf="errorMessage" class="hemo-report-viewer__error" role="alert">
          <svg class="hemo-report-viewer__error-icon" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8" />
            <line x1="12" y1="7.5" x2="12" y2="13" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
            <circle cx="12" cy="16.5" r="1.1" fill="currentColor" />
          </svg>
          <p class="hemo-report-viewer__error-title">{{ errorMessage }}</p>
          <p class="hemo-report-viewer__error-hint">กรุณาลองใหม่อีกครั้ง หรือตรวจสอบการเชื่อมต่อ</p>
        </div>
      </div>
    </div>
  `,
})
export class HemoReportViewerComponent implements OnChanges {
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('canvasHost', { read: ElementRef }) canvasHostRef?: ElementRef<HTMLElement>;

  @Input() pdfBlob: Blob | null = null;
  @Input() loading = false;
  @Input() errorMessage: string | null = null;
  @Input() printing = false;
  @Input() downloading = false;
  @Input() workerSrc = '/assets/pdfjs/pdf.worker.min.mjs';

  @Output() print = new EventEmitter<void>();
  @Output() download = new EventEmitter<void>();

  private readonly zoomFactor = signal(1);
  private readonly fitScale = signal(1);
  private readonly pdfPageWidth = signal(0);
  private readonly pdfPageCount = signal(1);

  readonly scale = computed(() => +(this.fitScale() * this.zoomFactor()).toFixed(3));
  readonly pageIndex = signal(0);
  readonly pageCount = computed(() => Math.max(this.pdfPageCount(), 1));

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pdfBlob']) {
      this.pageIndex.set(0);
      this.zoomFactor.set(1);
      this.pdfPageCount.set(1);
      this.pdfPageWidth.set(0);
    }
  }

  constructor() {
    afterNextRender(() => {
      this.updateFitScale();
      const host = this.canvasHostRef?.nativeElement;
      if (!host || typeof ResizeObserver === 'undefined') {
        return;
      }

      const observer = new ResizeObserver(() => this.updateFitScale());
      observer.observe(host);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  onPageCountChange(count: number): void {
    this.pdfPageCount.set(Math.max(count, 1));
    this.pageIndex.update((index) => Math.min(index, Math.max(count - 1, 0)));
  }

  onPageWidthChange(width: number): void {
    this.pdfPageWidth.set(width);
    this.updateFitScale();
  }

  zoomIn(): void {
    this.zoomFactor.update((value) => Math.min(2, +(value + 0.1).toFixed(2)));
  }

  zoomOut(): void {
    this.zoomFactor.update((value) => Math.max(0.5, +(value - 0.1).toFixed(2)));
  }

  prevPage(): void {
    this.pageIndex.update((index) => Math.max(0, index - 1));
  }

  nextPage(): void {
    this.pageIndex.update((index) => Math.min(this.pageCount() - 1, index + 1));
  }

  private updateFitScale(): void {
    const host = this.canvasHostRef?.nativeElement;
    const pageWidth = this.pdfPageWidth();
    if (!host || pageWidth <= 0) {
      return;
    }

    const horizontalPadding = 32;
    const available = Math.max(host.clientWidth - horizontalPadding, 1);
    this.fitScale.set(Math.min(1, available / pageWidth));
  }
}
