import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GeneratePdfRequest } from '../models/generate-pdf-request.model';
import { HemoPdfService } from '../services/hemo-pdf.service';

@Component({
  selector: 'hemo-pdf-download-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button type="button" [disabled]="disabled || loading" (click)="onClick()">
      {{ loading ? loadingLabel : label }}
    </button>
  `,
})
export class PdfDownloadButtonComponent {
  @Input() request!: GeneratePdfRequest;
  @Input() label = 'Generate PDF';
  @Input() loadingLabel = 'Generating...';
  @Input() disabled = false;
  @Input() mode: 'open' | 'download' = 'open';

  @Output() pdfOpened = new EventEmitter<void>();
  @Output() pdfError = new EventEmitter<unknown>();

  loading = false;

  constructor(private readonly pdfService: HemoPdfService) {}

  onClick(): void {
    if (!this.request || this.loading) return;

    this.loading = true;
    const action =
      this.mode === 'download'
        ? this.pdfService.download(this.request)
        : this.pdfService.generateAndOpen(this.request);

    action.subscribe({
      next: () => {
        this.loading = false;
        this.pdfOpened.emit();
      },
      error: (err) => {
        this.loading = false;
        this.pdfError.emit(err);
      },
    });
  }
}
