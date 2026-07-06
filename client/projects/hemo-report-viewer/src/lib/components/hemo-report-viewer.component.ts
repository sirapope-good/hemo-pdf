import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportDocument } from '../models/report-document.model';
import { HemoReportToolbarComponent } from './hemo-report-toolbar.component';
import { HemoReportPageComponent } from './hemo-report-page.component';

@Component({
  selector: 'hemo-report-viewer',
  standalone: true,
  imports: [CommonModule, HemoReportToolbarComponent, HemoReportPageComponent],
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

      <div class="hemo-report-viewer__canvas">
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
  @Input() document: ReportDocument | null = null;
  @Input() loading = false;
  @Input() errorMessage: string | null = null;

  @Output() print = new EventEmitter<void>();
  @Output() download = new EventEmitter<void>();

  readonly scale = signal(1);
  readonly pageIndex = signal(0);

  readonly pageCount = computed(() => {
    const pages = this.document?.pages?.length ?? 0;
    return Math.max(pages, 1);
  });

  zoomIn(): void {
    this.scale.update((value) => Math.min(2, +(value + 0.1).toFixed(2)));
  }

  zoomOut(): void {
    this.scale.update((value) => Math.max(0.5, +(value - 0.1).toFixed(2)));
  }

  prevPage(): void {
    this.pageIndex.update((index) => Math.max(0, index - 1));
  }

  nextPage(): void {
    this.pageIndex.update((index) => Math.min(this.pageCount() - 1, index + 1));
  }
}
