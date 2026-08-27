import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { LocationDto, LocationsClient } from '../../../core/api/generated/api-client';

/** Minimal read-only wrapper for the one place the dashboard needs a location picker
 * (the department/location-scoped announcement dialog) — a full locations admin facade belongs
 * to whichever future phase builds that screen. */
@Injectable({ providedIn: 'root' })
export class LocationsFacade {
  private readonly client = inject(LocationsClient);

  list(): Observable<LocationDto[]> {
    return this.client.locations_List(1, 100, undefined, false).pipe(map((result) => result.items ?? []));
  }
}
