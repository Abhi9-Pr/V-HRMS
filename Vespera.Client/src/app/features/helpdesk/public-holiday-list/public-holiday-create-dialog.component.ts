import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { catchError, map, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { HelpdeskFacade } from '../data/helpdesk.facade';

@Component({
  selector: 'vespera-public-holiday-create-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatButtonModule],
  templateUrl: './public-holiday-create-dialog.component.html',
})
export class PublicHolidayCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialogRef = inject(MatDialogRef<PublicHolidayCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    date: [new Date(), [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.helpdeskFacade
      .createHoliday(this.form.getRawValue())
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
