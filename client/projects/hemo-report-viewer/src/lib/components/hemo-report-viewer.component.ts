import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  ViewEncapsulation,
  afterNextRender,
  inject,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { ReportDocument } from '../models/report-document.model';
import { HemoReportToolbarComponent } from './hemo-report-toolbar.component';
import { HemoReportPageComponent } from './hemo-report-page.component';

const A4_WIDTH_MM = 210;
const MM_TO_PX = 96 / 25.4;

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
        (zoomIn)="zoomIn()"
        (zoomOut)="zoomOut()"
        (prevPage)="prevPage()"
        (nextPage)="nextPage()"
        (print)="print.emit()"
        (download)="download.emit()" />

      <div #canvas class="hemo-report-viewer__canvas">
        <div
          *ngIf="document"
          class="hemo-report-viewer__scale-wrap"
          [style.transform]="'scale(' + scale() + ')'">
          <hemo-report-page [document]="document" [pageIndex]="pageIndex()" />
        </div>
        <div *ngIf="!document && loading" class="hemo-report-viewer__loading">Loading preview…</div>
        <div *ngIf="errorMessage" class="hemo-report-viewer__error">{{ errorMessage }}</div>
      </div>
    </div>
  `,
})
export class HemoReportViewerComponent {
  private readonly destroyRef = inject(DestroyRef);
  private readonly documentRef = inject(DOCUMENT);

  @ViewChild('canvas', { read: ElementRef }) canvasRef?: ElementRef<HTMLElement>;

  @Input() document: ReportDocument | null = null;
  @Input() loading = false;
  @Input() errorMessage: string | null = null;

  @Output() print = new EventEmitter<void>();
  @Output() download = new EventEmitter<void>();

  private readonly zoomFactor = signal(1);
  private readonly fitScale = signal(1);

  readonly scale = computed(() => +(this.fitScale() * this.zoomFactor()).toFixed(3));
  readonly pageIndex = signal(0);

  readonly pageCount = computed(() => {
    const pages = this.document?.pages?.length ?? 0;
    return Math.max(pages, 1);
  });

  constructor() {
    afterNextRender(() => {
      this.updateFitScale();
      const canvas = this.canvasRef?.nativeElement;
      if (!canvas || typeof ResizeObserver === 'undefined') {
        return;
      }

      const observer = new ResizeObserver(() => this.updateFitScale());
      observer.observe(canvas);
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
  }

  nextPage(): void {
    this.pageIndex.update((index) => Math.min(this.pageCount() - 1, index + 1));
  }

  private updateFitScale(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) {
      return;
    }

    const pageWidthPx = A4_WIDTH_MM * MM_TO_PX;
    const horizontalPadding = 32;
    const available = Math.max(canvas.clientWidth - horizontalPadding, 1);
    this.fitScale.set(Math.min(1, available / pageWidthPx));
  }
}
