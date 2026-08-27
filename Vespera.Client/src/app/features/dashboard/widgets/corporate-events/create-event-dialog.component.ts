import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { catchError, of } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error.model';
import { CorporateEventsFacade } from '../../data/corporate-events.facade';

@Component({
  selector: 'vespera-create-event-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatButtonModule],
  templateUrl: './create-event-dialog.component.html',
})
export class CreateEventDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly corporateEventsFacade = inject(CorporateEventsFacade);
  private readonly dialogRef = inject(MatDialogRef<CreateEventDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(256)]],
    description: ['', [Validators.required]],
    locationText: ['', [Validators.required, Validators.maxLength(256)]],
    startsAt: [new Date(), [Validators.required]],
    endsAt: [new Date(Date.now() + 60 * 60 * 1000), [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();

    this.corporateEventsFacade
      .create(value)
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((id) => {
        if (id) {
          this.dialogRef.close(true);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
