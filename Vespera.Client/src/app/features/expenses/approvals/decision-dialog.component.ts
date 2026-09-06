import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { ExpensesFacade } from '../data/expenses.facade';
import { ApiError } from 'vespera-shared';

export interface DecisionDialogData {
  claimId: string;
  approved: boolean;
}

@Component({
  selector: 'vespera-decision-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './decision-dialog.component.html',
})
export class DecisionDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly expensesFacade = inject(ExpensesFacade);
  private readonly dialogRef = inject(MatDialogRef<DecisionDialogComponent>);

  readonly data = inject<DecisionDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    comment: [''],
  });

  submit(): void {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.expensesFacade
      .decideApproval(this.data.claimId, {
        approved: this.data.approved,
        comment: this.form.getRawValue().comment || undefined,
      })
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
          this.dialogRef.close(true);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
