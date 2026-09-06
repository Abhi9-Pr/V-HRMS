import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { TICKET_PRIORITY_LABELS } from '../helpdesk.labels';
import { ApiError, TicketPriority } from 'vespera-shared';

@Component({
  selector: 'vespera-ticket-create-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './ticket-create-dialog.component.html',
})
export class TicketCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialogRef = inject(MatDialogRef<TicketCreateDialogComponent>);

  readonly priorities = Object.values(TicketPriority).filter(
    (value): value is TicketPriority => typeof value === 'number',
  );
  readonly priorityLabel = (priority: TicketPriority): string => TICKET_PRIORITY_LABELS[priority];

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    categoryId: ['', [Validators.required]],
    subject: ['', [Validators.required, Validators.maxLength(256)]],
    description: ['', [Validators.required]],
    priority: [TicketPriority._1, [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.helpdeskFacade
      .raiseTicket(this.form.getRawValue())
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
