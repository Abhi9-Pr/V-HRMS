import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ExpensesService } from '../expenses.service';

@Component({
  selector: 'app-settlement',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './settlement.html',
  styleUrl: './settlement.scss',
})
export class Settlement {
  protected readonly success = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    expenseClaimId: ['', Validators.required],
    payrollRunId: ['', Validators.required],
  });

  constructor(private readonly expenses: ExpensesService) {}

  settle(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.success.set(null);
    this.error.set(null);

    const value = this.form.getRawValue();
    this.expenses.settleClaim(value).subscribe({
      next: () => this.success.set('Claim settled into the payroll run as a reimbursement line.'),
      error: (err: HttpErrorResponse) => this.error.set(err.error?.detail ?? 'Settlement failed.'),
    });
  }
}
