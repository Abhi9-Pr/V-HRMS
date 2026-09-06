import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { DateRange, DateRangePickerComponent } from '../../../shared/date-range-picker/date-range-picker.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LeaveFacade } from '../data/leave.facade';

function startOfWeek(date: Date): Date {
  const result = new Date(date);
  result.setDate(result.getDate() - result.getDay());
  return result;
}

/** A day-by-day list rather than a month grid — it shows exactly the same information (who's out,
 * and where the team crosses the conflict threshold) without a bespoke calendar-grid component
 * this codebase doesn't have one of yet. */
@Component({
    selector: 'vespera-team-calendar',
    imports: [DateRangePickerComponent, MatChipsModule, MatIconModule, ErrorStateComponent, DatePipe],
    templateUrl: './team-calendar.component.html'
})
export class TeamCalendarComponent implements OnInit {
  private readonly leaveFacade = inject(LeaveFacade);

  readonly days = this.leaveFacade.teamCalendar;
  readonly loading = this.leaveFacade.loading;
  readonly error = this.leaveFacade.error;

  private readonly range = signal<DateRange>({ start: startOfWeek(new Date()), end: addDays(startOfWeek(new Date()), 13) });

  ngOnInit(): void {
    this.reload();
  }

  onRangeChange(range: DateRange): void {
    this.range.set(range);
    this.reload();
  }

  reload(): void {
    const { start, end } = this.range();
    if (start && end) {
      this.leaveFacade.loadTeamCalendar(start, end);
    }
  }
}

function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}
