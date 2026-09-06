import { Component, Input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PermissionButtonComponent } from '../../../../shared/buttons/permission-button.component';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { rsvpResponseLabels } from '../../dashboard.labels';
import { CorporateEventsFacade } from '../../data/corporate-events.facade';
import { CreateEventDialogComponent } from './create-event-dialog.component';
import { CorporateEventSummaryDto, Permissions, RsvpResponse } from 'vespera-shared';

@Component({
  selector: 'vespera-corporate-events-widget',
  imports: [PermissionButtonComponent, TimezoneDatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './corporate-events-widget.component.html',
})
export class CorporateEventsWidgetComponent {
  private readonly corporateEventsFacade = inject(CorporateEventsFacade);
  private readonly dialog = inject(MatDialog);

  readonly Permissions = Permissions;
  readonly rsvpResponseLabels = rsvpResponseLabels;
  readonly RsvpResponse = RsvpResponse;

  private readonly itemsSignal = signal<CorporateEventSummaryDto[]>([]);
  readonly items = this.itemsSignal.asReadonly();

  @Input({ required: true })
  set data(value: CorporateEventSummaryDto[]) {
    this.itemsSignal.set(value ?? []);
  }

  rsvp(item: CorporateEventSummaryDto, response: RsvpResponse): void {
    if (!item.id) {
      return;
    }

    this.corporateEventsFacade.rsvp(item.id, response).subscribe(() => {
      this.itemsSignal.update((items) =>
        items.map((i) => (i.id === item.id ? { ...i, myResponse: rsvpResponseLabels[response] } : i)),
      );
    });
  }

  openCreateDialog(): void {
    this.dialog.open(CreateEventDialogComponent, { width: '520px' });
  }
}
