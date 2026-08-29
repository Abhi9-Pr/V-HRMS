import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CorporateEventSummaryDto,
  CorporateEventsClient,
  CreateCorporateEventRequest,
  RsvpResponse,
} from 'vespera-shared';

@Injectable({ providedIn: 'root' })
export class CorporateEventsFacade {
  private readonly client = inject(CorporateEventsClient);

  upcoming(): Observable<CorporateEventSummaryDto[]> {
    return this.client.corporateEvents_Upcoming();
  }

  create(request: CreateCorporateEventRequest): Observable<string> {
    return this.client.corporateEvents_Create(request);
  }

  rsvp(id: string, response: RsvpResponse): Observable<void> {
    return this.client.corporateEvents_Rsvp(id, response);
  }
}
