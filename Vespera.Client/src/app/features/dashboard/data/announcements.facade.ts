import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AnnouncementReceiptsReportDto,
  AnnouncementSummaryDto,
  AnnouncementsClient,
  CreateAnnouncementRequest,
} from 'vespera-shared';

@Injectable({ providedIn: 'root' })
export class AnnouncementsFacade {
  private readonly client = inject(AnnouncementsClient);

  forMe(): Observable<AnnouncementSummaryDto[]> {
    return this.client.announcements_ForMe();
  }

  create(request: CreateAnnouncementRequest): Observable<string> {
    return this.client.announcements_Create(request);
  }

  acknowledge(id: string): Observable<void> {
    return this.client.announcements_Acknowledge(id);
  }

  setPinned(id: string, pinned: boolean): Observable<void> {
    return this.client.announcements_SetPinned(id, pinned);
  }

  receiptsReport(id: string): Observable<AnnouncementReceiptsReportDto> {
    return this.client.announcements_ReceiptsReport(id);
  }
}
