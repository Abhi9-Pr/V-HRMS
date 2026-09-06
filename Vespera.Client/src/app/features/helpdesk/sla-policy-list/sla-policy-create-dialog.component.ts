import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { ApiError } from 'vespera-shared';

/** Backend TimeOnly fields serialize as "HH:mm:ss" — there's no native Material time picker in
 * this codebase, so a plain `<input type="time">` (which yields "HH:mm") is padded to "HH:mm:ss"
 * on submit, same as this feature's only other TimeOnly-shaped input. */
function toTimeOnly(value: string): string {
  return value.length === 5 ? `${value}:00` : value;
}

@Component({
  selector: 'vespera-sla-policy-create-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './sla-policy-create-dialog.component.html',
})
export class SlaPolicyCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialogRef = inject(MatDialogRef<SlaPolicyCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    responseTimeHours: [4, [Validators.required, Validators.min(1)]],
    resolutionTimeHours: [24, [Validators.required, Validators.min(1)]],
    businessHoursStart: ['09:00', [Validators.required]],
    businessHoursEnd: ['18:00', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const { name, responseTimeHours, resolutionTimeHours, businessHoursStart, businessHoursEnd } =
      this.form.getRawValue();

    this.helpdeskFacade
      .createPolicy({
        name,
        responseTimeHours,
        resolutionTimeHours,
        businessHoursStart: toTimeOnly(businessHoursStart),
        businessHoursEnd: toTimeOnly(businessHoursEnd),
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
