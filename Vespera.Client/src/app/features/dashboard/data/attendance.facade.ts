import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AttendanceClient, RecordWebPunchRequest } from '../../../core/api/generated/api-client';

/**
 * Thin wrapper for the shift-tracker widget's quick-punch button — the rest of self-service
 * attendance (regularizations, team calendar, biometric admin) has no Angular feature yet, so
 * this stays scoped to exactly the one action the dashboard needs rather than standing up a full
 * `features/attendance` module ahead of that phase (see AGENTS.md's working agreement).
 */
@Injectable({ providedIn: 'root' })
export class AttendanceFacade {
  private readonly client = inject(AttendanceClient);

  punch(employeeId: string, punchType: 'In' | 'Out', coordinates?: { latitude: number; longitude: number }): Observable<void> {
    const request: RecordWebPunchRequest = {
      employeeId,
      punchType,
      latitude: coordinates?.latitude,
      longitude: coordinates?.longitude,
    };
    return this.client.attendance_Punch(request);
  }
}
