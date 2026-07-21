import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignatureReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-signature-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block hemo-signature-block">
      <div class="hemo-signature-block__slots">
        <div *ngFor="let slot of block.slots" class="hemo-signature-block__slot">
          <div>{{ slot.role }}</div>
          <img *ngIf="slot.imageUrl" [src]="slot.imageUrl" alt="" class="hemo-signature-block__image" />
          <div *ngIf="slot.name">{{ slot.name }}</div>
          <div *ngIf="slot.signedAt">{{ slot.signedAt }}</div>
        </div>
      </div>
    </section>
  `,
})
export class SignatureBlockComponent {
  @Input({ required: true }) block!: SignatureReportBlock;
}
