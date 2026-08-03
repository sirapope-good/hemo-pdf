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
import { ReportDocument } from '../models/report-document.model';
import { HemoReportToolbarComponent } from './hemo-report-toolbar.component';
import { HemoReportPageComponent } from './hemo-report-page.component';

const A4_WIDTH_MM = 210;
const A4_HEIGHT_MM = 297;
const MM_TO_PX = 96 / 25.4;
const PAGE_WIDTH_PX = A4_WIDTH_MM * MM_TO_PX;
const PAGE_HEIGHT_PX = A4_HEIGHT_MM * MM_TO_PX;

@Component({
  selector: 'hemo-report-viewer',
  standalone: true,
  imports: [CommonModule, HemoReportToolbarComponent, HemoReportPageComponent],
  encapsulation: ViewEncapsulation.None,
  styleUrls: ['../styles/report-viewer.scss'],
  template: `
    <div class="hemo-report-viewer">
      <hemo-report-toolbar
        [scale]="scale()"
        [pageIndex]="pageIndex()"
        [pageCount]="pageCount()"
        [minScale]="minDisplayScale()"
        [maxScale]="maxDisplayScale()"
        [loading]="loading"
        [printing]="printing"
        [downloading]="downloading"
        (zoomIn)="zoomIn()"
        (zoomOut)="zoomOut()"
        (prevPage)="prevPage()"
        (nextPage)="nextPage()"
        (reload)="reload.emit()"
        (print)="print.emit()"
        (download)="download.emit()" />

      <div #canvasHost class="hemo-report-viewer__canvas">
        <div
          *ngIf="document"
          class="hemo-report-viewer__scale-slot"
          [style.width.px]="scaledWidth()"
          [style.height.px]="scaledHeight()">
          <div
            class="hemo-report-viewer__scale-wrap"
            [style.width.px]="pageWidthPx"
            [style.height.px]="pageHeightPx"
            [style.transform]="'scale(' + scale() + ')'">
            <hemo-report-page [document]="document" [pageIndex]="pageIndex()" />
          </div>
        </div>
        <div *ngIf="!document && loading" class="hemo-report-viewer__loading">
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

  @Input() document: ReportDocument | null = null;
  @Input() loading = false;
  @Input() errorMessage: string | null = null;
  @Input() printing = false;
  @Input() downloading = false;

  @Output() print = new EventEmitter<void>();
  @Output() download = new EventEmitter<void>();
  @Output() reload = new EventEmitter<void>();

  readonly pageWidthPx = PAGE_WIDTH_PX;
  readonly pageHeightPx = PAGE_HEIGHT_PX;

  private readonly zoomFactor = signal(1);
  private readonly fitScale = signal(1);

  readonly scale = computed(() => +(this.fitScale() * this.zoomFactor()).toFixed(3));
  readonly scaledWidth = computed(() => Math.ceil(PAGE_WIDTH_PX * this.scale()));
  readonly scaledHeight = computed(() => Math.ceil(PAGE_HEIGHT_PX * this.scale()));
  readonly minDisplayScale = computed(() => +(this.fitScale() * 0.5).toFixed(3));
  readonly maxDisplayScale = computed(() => +(this.fitScale() * 2).toFixed(3));
  readonly pageIndex = signal(0);

  readonly pageCount = computed(() => {
    const pages = this.document?.pages?.length ?? 0;
    return Math.max(pages, 1);
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['document']) {
      this.pageIndex.set(0);
      this.zoomFactor.set(1);
      this.resetScroll();
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

  zoomIn(): void {
    this.zoomFactor.update((value) => Math.min(2, +(value + 0.1).toFixed(2)));
  }

  zoomOut(): void {
    this.zoomFactor.update((value) => Math.max(0.5, +(value - 0.1).toFixed(2)));
  }

  prevPage(): void {
    this.pageIndex.update((index) => Math.max(0, index - 1));
    this.resetScroll();
  }

  nextPage(): void {
    this.pageIndex.update((index) => Math.min(this.pageCount() - 1, index + 1));
    this.resetScroll();
  }

  private updateFitScale(): void {
    const host = this.canvasHostRef?.nativeElement;
    if (!host) {
      return;
    }

    const horizontalPadding = 32;
    const available = Math.max(host.clientWidth - horizontalPadding, 1);
    // Fit to container width (may exceed 100% on wide viewports so the page isn't tiny).
    this.fitScale.set(Math.min(2.5, available / PAGE_WIDTH_PX));
  }

  private resetScroll(): void {
    const host = this.canvasHostRef?.nativeElement;
    if (!host) {
      return;
    }
    host.scrollLeft = 0;
    host.scrollTop = 0;
  }
}
