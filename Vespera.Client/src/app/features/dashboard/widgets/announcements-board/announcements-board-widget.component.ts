import { Component, Input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { PermissionButtonComponent } from '../../../../shared/buttons/permission-button.component';
import { MarkdownPipe } from '../../../../shared/pipes/markdown.pipe';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { priorityLabelByName } from '../../dashboard.labels';
import { AnnouncementsFacade } from '../../data/announcements.facade';
import { CreateAnnouncementDialogComponent } from './create-announcement-dialog.component';
import { ReadReceiptsDialogComponent } from './read-receipts-dialog.component';
import { AnnouncementSummaryDto, Permissions } from 'vespera-shared';

@Component({
  selector: 'vespera-announcements-board-widget',
  imports: [MatIconModule, PermissionButtonComponent, MarkdownPipe, TimezoneDatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './announcements-board-widget.component.html',
})
export class AnnouncementsBoardWidgetComponent {
  private readonly announcementsFacade = inject(AnnouncementsFacade);
  private readonly dialog = inject(MatDialog);

  readonly Permissions = Permissions;
  readonly priorityLabelByName = priorityLabelByName;

  private readonly itemsSignal = signal<AnnouncementSummaryDto[]>([]);
  readonly items = this.itemsSignal.asReadonly();

  @Input({ required: true })
  set data(value: AnnouncementSummaryDto[]) {
    this.itemsSignal.set(value ?? []);
  }

  acknowledge(item: AnnouncementSummaryDto): void {
    if (!item.id || item.isAcknowledged) {
      return;
    }

    this.announcementsFacade.acknowledge(item.id).subscribe(() => {
      this.itemsSignal.update((items) => items.map((i) => (i.id === item.id ? { ...i, isAcknowledged: true } : i)));
    });
  }

  openCreateDialog(): void {
    this.dialog.open(CreateAnnouncementDialogComponent, { width: '520px' });
  }

  openReceiptsReport(item: AnnouncementSummaryDto): void {
    if (!item.id) {
      return;
    }
    this.dialog.open(ReadReceiptsDialogComponent, { width: '560px', data: { announcementId: item.id } });
  }
}
