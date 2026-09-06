import { Component, Input, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { AttendanceFacade } from '../../data/attendance.facade';
import { ShiftTrackerWidgetDto } from '../dashboard-widget-payloads.model';

@Component({
  selector: 'vespera-shift-tracker-widget',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, TimezoneDatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './shift-tracker-widget.component.html',
})
export class ShiftTrackerWidgetComponent {
  private readonly attendanceFacade = inject(AttendanceFacade);

  @Input({ required: true }) data!: ShiftTrackerWidgetDto;

  readonly punching = signal(false);
  readonly punchError = signal<string | null>(null);
  private readonly localOverride = signal<'In' | 'Out' | null>(null);

  readonly effectiveStatus = computed(() => this.localOverride() ?? this.data.punchStatus);

  readonly nextPunchType = computed<'In' | 'Out'>(() => (this.effectiveStatus() === 'In' ? 'Out' : 'In'));

  quickPunch(): void {
    if (!this.data.employeeId || this.punching()) {
      return;
    }

    this.punching.set(true);
    this.punchError.set(null);
    const punchType = this.nextPunchType();

    this.attendanceFacade.punch(this.data.employeeId, punchType).subscribe({
      next: () => {
        this.localOverride.set(punchType);
        this.punching.set(false);
      },
      error: () => {
        this.punchError.set("Couldn't record that punch. Try again.");
        this.punching.set(false);
      },
    });
  }
}
