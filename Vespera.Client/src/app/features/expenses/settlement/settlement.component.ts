import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { ExpensesFacade } from '../data/expenses.facade';

/** No payroll-run picker exists anywhere in this client yet (Payroll is a separate,
 * NgRx-backed feature slice per docs/frontend-state.md) — this takes the run's id as a plain
 * text input, the same "no picker exists, take the raw id" treatment used elsewhere in this
 * codebase wherever a cross-feature reference has no UI to look it up yet. */
@Component({
  selector: 'vespera-settlement',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, FormErrorComponent],
  templateUrl: './settlement.component.html',
})
export class SettlementComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly expensesFacade = inject(ExpensesFacade);

  readonly settling = signal(false);
  readonly error = signal<string | null>(null);
  readonly settled = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    expenseClaimId: ['', [Validators.required]],
    payrollRunId: ['', [Validators.required]],
  });

  settle(): void {
    if (this.form.invalid || this.settling()) {
      this.form.markAllAsTouched();
      return;
    }

    this.settling.set(true);
    this.error.set(null);
    this.settled.set(false);

    this.expensesFacade
      .settleClaim(this.form.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.settling.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.settling.set(false);
        this.settled.set(true);
        this.form.reset({ expenseClaimId: '', payrollRunId: '' });
      });
  }
}
