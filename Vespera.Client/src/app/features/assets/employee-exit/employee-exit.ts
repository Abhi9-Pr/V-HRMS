import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { AssetsService } from '../assets.service';
import { EMPLOYEE_EXIT_REASON_LABELS, EmployeeExitReason } from '../assets.models';

@Component({
  selector: 'app-employee-exit',
  imports: [ReactiveFormsModule, MatButtonModule, MatDatepickerModule, MatFormFieldModule, MatInputModule, MatNativeDateModule, MatSelectModule],
  templateUrl: './employee-exit.html',
  styleUrl: './employee-exit.scss',
})
export class EmployeeExit {
  protected readonly success = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly reasonOptions = [
    EmployeeExitReason.Resignation,
    EmployeeExitReason.Termination,
    EmployeeExitReason.Retirement,
    EmployeeExitReason.EndOfContract,
  ];

  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
    exitDate: [new Date(), Validators.required],
    reason: [EmployeeExitReason.Resignation, Validators.required],
  });

  constructor(private readonly assetsService: AssetsService) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.success.set(null);
    this.error.set(null);

    const value = this.form.getRawValue();
    this.assetsService.exitEmployee(value.employeeId, { exitDate: toDateOnly(value.exitDate), reason: value.reason }).subscribe({
      next: () =>
        this.success.set(
          'Exit recorded. Held license seats will be released and asset recovery initiated automatically.',
        ),
      error: (err: HttpErrorResponse) => this.error.set(err.error?.detail ?? 'Recording the exit failed.'),
    });
  }

  protected reasonLabel(reason: EmployeeExitReason): string {
    return EMPLOYEE_EXIT_REASON_LABELS[reason];
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
