import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { catchError, map, of } from 'rxjs';
import { DateRange, DateRangePickerComponent } from '../../../shared/date-range-picker/date-range-picker.component';
import { ConfirmDialogComponent } from '../../../shared/dialogs/confirm-dialog.component';
import { LeaveFacade } from '../data/leave.facade';
import { ApiError } from 'vespera-shared';

const SCOPES = ['LeaveApprovals', 'ExpenseApprovals', 'AttendanceApprovals', 'All'] as const;

/** "Holiday Mode" — a manager routes their approvals to someone else for a date window. */
@Component({
  selector: 'vespera-delegation-settings',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatChipsModule,
    DateRangePickerComponent,
    DatePipe,
  ],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './delegation-settings.component.html',
})
export class DelegationSettingsComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly leaveFacade = inject(LeaveFacade);
  private readonly dialog = inject(MatDialog);

  readonly employees = this.leaveFacade.employees;
  readonly delegations = this.leaveFacade.delegations;
  readonly scopes = SCOPES;
  readonly displayedColumns = ['delegate', 'dates', 'scope', 'status', 'actions'];

  readonly form = this.formBuilder.nonNullable.group({
    delegateEmployeeId: ['', Validators.required],
    scope: ['LeaveApprovals', Validators.required],
  });

  private readonly selectedRange = signal<DateRange>({ start: null, end: null });

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.leaveFacade.loadEmployees();
    this.leaveFacade.loadMyDelegations();
  }

  onRangeChange(range: DateRange): void {
    this.selectedRange.set(range);
  }

  create(): void {
    const { start, end } = this.selectedRange();
    if (this.form.invalid || !start || !end || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const { delegateEmployeeId, scope } = this.form.getRawValue();

    this.leaveFacade
      .createDelegation({ delegateEmployeeId, from: start, to: end, scope })
      .pipe(
        map(() => true),
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(false);
        }),
      )
      .subscribe((succeeded) => {
        if (succeeded) {
          this.saving.set(false);
          this.form.reset({ delegateEmployeeId: '', scope: 'LeaveApprovals' });
          this.selectedRange.set({ start: null, end: null });
          this.leaveFacade.loadMyDelegations();
        }
      });
  }

  revoke(delegationId: string | undefined): void {
    if (!delegationId) {
      return;
    }

    this.dialog
      .open(ConfirmDialogComponent, {
        data: { title: 'Revoke delegation', message: 'Revoke this delegation?', danger: true },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.leaveFacade.revokeDelegation(delegationId).subscribe(() => this.leaveFacade.loadMyDelegations());
        }
      });
  }

  employeeLabel(employeeId: string | undefined): string {
    return this.employees().find((employee) => employee.id === employeeId)?.fullName ?? employeeId ?? '';
  }
}
