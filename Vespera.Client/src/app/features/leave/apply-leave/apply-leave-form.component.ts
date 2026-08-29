import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { DateRange, DateRangePickerComponent } from '../../../shared/date-range-picker/date-range-picker.component';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { LeaveFacade } from '../data/leave.facade';
import { ApiError } from 'vespera-shared';

/** Excludes weekends and any date in `holidays` — the same rule
 * `Vespera.Domain.Services.LeaveDayCounter` applies server-side with the sandwich rule off. A
 * policy with the sandwich rule on may charge more than this preview shows; the server's
 * response after submit is always the number of record. */
function countPreviewDays(start: Date, end: Date, holidays: ReadonlySet<string>): number {
  let count = 0;
  for (let date = new Date(start); date <= end; date.setDate(date.getDate() + 1)) {
    const isWeekend = date.getDay() === 0 || date.getDay() === 6;
    if (!isWeekend && !holidays.has(toIsoDate(date))) {
      count++;
    }
  }
  return count;
}

function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

@Component({
  selector: 'vespera-apply-leave-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    DateRangePickerComponent,
    FormErrorComponent,
  ],
  templateUrl: './apply-leave-form.component.html',
})
export class ApplyLeaveFormComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly leaveFacade = inject(LeaveFacade);

  readonly leaveTypes = this.leaveFacade.leaveTypes;
  readonly balance = this.leaveFacade.balance;

  readonly form = this.formBuilder.nonNullable.group({
    leaveTypeId: ['', Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(1000)]],
    acknowledgeInsufficientBalance: [false],
  });

  private readonly selectedRange = signal<DateRange>({ start: null, end: null });
  private readonly holidayDates = computed(
    () => new Set((this.leaveFacade.holidays() ?? []).map((h) => (h.date ? toIsoDate(new Date(h.date)) : ''))),
  );

  readonly previewDays = computed(() => {
    const { start, end } = this.selectedRange();
    return start && end ? countPreviewDays(start, end, this.holidayDates()) : 0;
  });

  readonly projectedShortfall = computed(() => {
    const available = this.balance()?.available;
    return available === undefined ? 0 : Math.max(0, this.previewDays() - available);
  });

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly submittedResult = signal<{ requestedDays: number; lossOfPayDays: number } | null>(null);

  ngOnInit(): void {
    this.leaveFacade.loadLeaveTypes();
    this.leaveFacade.loadHolidays();

    this.form.controls.leaveTypeId.valueChanges.subscribe((leaveTypeId) => {
      if (leaveTypeId) {
        this.leaveFacade.loadBalance(leaveTypeId);
      }
    });
  }

  onRangeChange(range: DateRange): void {
    this.selectedRange.set(range);
  }

  submit(): void {
    const { start, end } = this.selectedRange();
    if (this.form.invalid || !start || !end || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.submittedResult.set(null);

    const { leaveTypeId, reason, acknowledgeInsufficientBalance } = this.form.getRawValue();

    this.leaveFacade
      .submit({ leaveTypeId, from: start, to: end, reason, acknowledgeInsufficientBalance })
      .pipe(
        map((response) => ({ requestedDays: response.requestedDays ?? 0, lossOfPayDays: response.lossOfPayDays ?? 0 })),
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.submitting.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result) {
          this.submitting.set(false);
          this.submittedResult.set(result);
          this.form.reset({ leaveTypeId: '', reason: '', acknowledgeInsufficientBalance: false });
          this.selectedRange.set({ start: null, end: null });
        }
      });
  }
}
