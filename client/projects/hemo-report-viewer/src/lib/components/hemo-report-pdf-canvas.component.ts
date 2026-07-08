import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

interface PdfPageViewport {
  width: number;
  height: number;
}

interface PdfPageProxy {
  getViewport(params: { scale: number }): PdfPageViewport;
  render(params: {
    canvasContext: CanvasRenderingContext2D;
    viewport: PdfPageViewport;
    canvas: HTMLCanvasElement;
  }): { promise: Promise<void>; cancel(): void };
}

interface PdfDocumentProxy {
  numPages: number;
  getPage(pageNumber: number): Promise<PdfPageProxy>;
  destroy(): Promise<void>;
}

interface PdfJsModule {
  GlobalWorkerOptions: { workerSrc: string };
  getDocument(src: { data: ArrayBuffer }): { promise: Promise<PdfDocumentProxy> };
}

let pdfJsModulePromise: Promise<PdfJsModule> | null = null;

function loadPdfJs(workerSrc: string): Promise<PdfJsModule> {
  if (!pdfJsModulePromise) {
    pdfJsModulePromise = import('pdfjs-dist').then((pdfjs) => {
      const module = pdfjs as unknown as PdfJsModule;
      module.GlobalWorkerOptions.workerSrc = workerSrc;
      return module;
    });
  }
  return pdfJsModulePromise;
}

@Component({
  selector: 'hemo-report-pdf-canvas',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="hemo-report-pdf-canvas">
      <canvas #pageCanvas class="hemo-report-pdf-canvas__page"></canvas>
    </div>
  `,
})
export class HemoReportPdfCanvasComponent implements OnChanges, OnDestroy {
  @ViewChild('pageCanvas', { static: true }) pageCanvasRef!: ElementRef<HTMLCanvasElement>;

  @Input() pdfBlob: Blob | null = null;
  @Input() pageIndex = 0;
  @Input() scale = 1;
  @Input() workerSrc = '/assets/pdfjs/pdf.worker.min.mjs';

  @Output() pageCountChange = new EventEmitter<number>();
  @Output() pageWidthChange = new EventEmitter<number>();

  private pdfDocument: PdfDocumentProxy | null = null;
  private loadGeneration = 0;
  private renderTask: { promise: Promise<void>; cancel(): void } | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pdfBlob']) {
      void this.loadDocument();
      return;
    }

    if (changes['pageIndex'] || changes['scale']) {
      void this.renderCurrentPage();
    }
  }

  ngOnDestroy(): void {
    this.loadGeneration += 1;
    this.cancelRender();
    void this.pdfDocument?.destroy();
    this.pdfDocument = null;
  }

  private async loadDocument(): Promise<void> {
    const generation = ++this.loadGeneration;
    this.cancelRender();
    await this.pdfDocument?.destroy();
    this.pdfDocument = null;

    if (!this.pdfBlob) {
      this.pageCountChange.emit(1);
      this.clearCanvas();
      return;
    }

    try {
      const pdfjs = await loadPdfJs(this.workerSrc);
      if (generation !== this.loadGeneration) {
        return;
      }

      const bytes = await this.pdfBlob.arrayBuffer();
      if (generation !== this.loadGeneration) {
        return;
      }

      this.pdfDocument = await pdfjs.getDocument({ data: bytes }).promise;
      if (generation !== this.loadGeneration) {
        await this.pdfDocument.destroy();
        this.pdfDocument = null;
        return;
      }

      this.pageCountChange.emit(this.pdfDocument.numPages);
      await this.renderCurrentPage(generation);
    } catch (error) {
      console.error('[HemoReportPdfCanvas] Failed to load PDF', error);
      this.pageCountChange.emit(1);
      this.clearCanvas();
    }
  }

  private async renderCurrentPage(expectedGeneration = this.loadGeneration): Promise<void> {
    if (!this.pdfDocument || expectedGeneration !== this.loadGeneration) {
      return;
    }

    const pageNumber = Math.min(
      Math.max(this.pageIndex + 1, 1),
      this.pdfDocument.numPages,
    );

    this.cancelRender();

    try {
      const page = await this.pdfDocument.getPage(pageNumber);
      if (expectedGeneration !== this.loadGeneration) {
        return;
      }

      const baseViewport = page.getViewport({ scale: 1 });
      this.pageWidthChange.emit(baseViewport.width);

      const viewport = page.getViewport({ scale: this.scale });
      const canvas = this.pageCanvasRef.nativeElement;
      const context = canvas.getContext('2d');
      if (!context) {
        return;
      }

      canvas.width = Math.floor(viewport.width);
      canvas.height = Math.floor(viewport.height);

      this.renderTask = page.render({
        canvasContext: context,
        viewport,
        canvas,
      });
      await this.renderTask.promise;
      this.renderTask = null;
    } catch (error) {
      if ((error as { name?: string })?.name === 'RenderingCancelledException') {
        return;
      }
      console.error('[HemoReportPdfCanvas] Failed to render page', error);
    }
  }

  private cancelRender(): void {
    this.renderTask?.cancel();
    this.renderTask = null;
  }

  private clearCanvas(): void {
    const canvas = this.pageCanvasRef?.nativeElement;
    if (!canvas) {
      return;
    }
    const context = canvas.getContext('2d');
    context?.clearRect(0, 0, canvas.width, canvas.height);
    canvas.width = 0;
    canvas.height = 0;
  }
}
