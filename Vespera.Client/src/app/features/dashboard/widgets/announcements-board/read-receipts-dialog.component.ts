import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { AnnouncementsFacade } from '../../data/announcements.facade';
import { AnnouncementReceiptsReportDto, ApiError } from 'vespera-shared';

/** HR compliance view: who in an announcement's audience has (and hasn't) acknowledged it — see
 * GetAnnouncementReceiptsReportQueryHandler. Opened from the announcements widget, gated by
 * Workspace.ManageAnnouncements the same as the "New announcement" action. */
@Component({
  selector: 'vespera-read-receipts-dialog',
  imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, TimezoneDatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './read-receipts-dialog.component.html',
})
export class ReadReceiptsDialogComponent implements OnInit {
  private readonly announcementsFacade = inject(AnnouncementsFacade);
  private readonly data = inject<{ announcementId: string }>(MAT_DIALOG_DATA);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly report = signal<AnnouncementReceiptsReportDto | null>(null);

  ngOnInit(): void {
    this.announcementsFacade.receiptsReport(this.data.announcementId).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (apiError: ApiError) => {
        this.error.set(apiError.message);
        this.loading.set(false);
      },
    });
  }
}
