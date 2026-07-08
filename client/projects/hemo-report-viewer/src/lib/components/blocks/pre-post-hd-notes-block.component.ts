import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrePostHdNotesReportBlock } from '../../models/report-document.model';

@Component({
  selector: 'hemo-pre-post-hd-notes-block',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="hemo-report-block">
      <table class="hemo-pre-post-hd-notes">
        <tbody>
          <tr>
            <td>
              <div class="hemo-pre-post-hd-notes__label">Pre HD</div>
              <div>{{ block.preHdContent || '—' }}</div>
            </td>
            <td class="hemo-pre-post-hd-notes__signer">{{ block.preHdSigner || '—' }}</td>
          </tr>
          <tr>
            <td>
              <div class="hemo-pre-post-hd-notes__label">Post HD</div>
              <div>{{ block.postHdContent || '—' }}</div>
            </td>
            <td class="hemo-pre-post-hd-notes__signer">{{ block.postHdSigner || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
})
export class PrePostHdNotesBlockComponent {
  @Input({ required: true }) block!: PrePostHdNotesReportBlock;
}
