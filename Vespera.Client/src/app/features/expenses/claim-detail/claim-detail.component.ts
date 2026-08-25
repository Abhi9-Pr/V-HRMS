import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { Currency, ExpenseClaimStatus } from '../../../core/api/generated/api-client';
import { Permissions } from '../../../core/authorization/permissions';
import { ApiError } from '../../../core/http/api-error.model';
import { HasPermissionDirective } from '../../../core/directives/has-permission.directive';
import { FileUploaderComponent } from '../../../shared/file-uploader/file-uploader.component';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { CurrencyDisplayPipe } from '../../../shared/pipes/currency-display.pipe';
import { TimezoneDatePipe } from '../../../shared/pipes/timezone-date.pipe';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { ExpensesFacade } from '../data/expenses.facade';
import { CURRENCY_LABELS, EXPENSE_CLAIM_STATUS_LABELS } from '../expenses.labels';

@Component({
  selector: 'vespera-claim-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    FileUploaderComponent,
    FormErrorComponent,
    PermissionButtonComponent,
    HasPermissionDirective,
    ErrorStateComponent,
    LoadingStateComponent,
    CurrencyDisplayPipe,
    TimezoneDatePipe,
  ],
  templateUrl: './claim-detail.component.html',
})
export class ClaimDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly expensesFacade = inject(ExpensesFacade);

  readonly Permissions = Permissions;
  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];
  readonly statusLabel = (status: ExpenseClaimStatus): string => EXPENSE_CLAIM_STATUS_LABELS[status];

  readonly claim = this.expensesFacade.claim;
  readonly loading = this.expensesFacade.claimLoading;
  readonly error = this.expensesFacade.claimError;

  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly receiptReference = signal<string | null>(null);
  readonly ocrConfidence = signal<number | null>(null);

  readonly addingLine = signal(false);
  readonly addLineError = signal<string | null>(null);

  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly submitWarnings = signal<string[]>([]);

  private claimId = '';

  readonly lineForm = this.formBuilder.nonNullable.group({
    category: ['', [Validators.required, Validators.maxLength(100)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currency: [Currency._0, [Validators.required]],
    expenseDate: [new Date(), [Validators.required]],
    vendor: [''],
    taxAmount: [null as number | null],
  });

  ngOnInit(): void {
    this.claimId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.expensesFacade.loadClaimById(this.claimId);
  }

  onFilesSelected(files: File[]): void {
    const file = files[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.expensesFacade
      .uploadReceipt(this.claimId, file)
      .pipe(
        catchError((apiError: ApiError) => {
          this.uploadError.set(apiError.message);
          this.uploading.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.uploading.set(false);
        this.receiptReference.set(result.receiptReference ?? null);

        const suggestions = result.suggestions;
        this.ocrConfidence.set(suggestions?.confidence ?? null);
        if (suggestions) {
          const current = this.lineForm.getRawValue();
          this.lineForm.patchValue({
            vendor: suggestions.vendor ?? current.vendor,
            expenseDate: suggestions.expenseDate ?? current.expenseDate,
            taxAmount: suggestions.taxAmount ?? current.taxAmount,
            amount: suggestions.amount ?? current.amount,
          });
        }
      });
  }

  addLine(): void {
    if (this.lineForm.invalid || this.addingLine()) {
      this.lineForm.markAllAsTouched();
      return;
    }

    this.addingLine.set(true);
    this.addLineError.set(null);

    const { category, amount, currency, expenseDate, vendor, taxAmount } = this.lineForm.getRawValue();

    this.expensesFacade
      .addLine(this.claimId, {
        category,
        amount,
        currency,
        expenseDate,
        vendor: vendor || undefined,
        taxAmount: taxAmount ?? undefined,
        receiptReference: this.receiptReference() ?? undefined,
      })
      .pipe(
        catchError((apiError: ApiError) => {
          this.addLineError.set(apiError.message);
          this.addingLine.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.addingLine.set(false);
        this.receiptReference.set(null);
        this.ocrConfidence.set(null);
        this.lineForm.reset({ category: '', amount: 0, currency: Currency._0, expenseDate: new Date(), vendor: '', taxAmount: null });
        this.reload();
      });
  }

  submitClaim(): void {
    this.submitting.set(true);
    this.submitError.set(null);
    this.submitWarnings.set([]);

    this.expensesFacade
      .submitClaim(this.claimId)
      .pipe(
        catchError((apiError: ApiError) => {
          this.submitError.set(apiError.message);
          this.submitting.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.submitting.set(false);
        this.submitWarnings.set(result.warnings ?? []);
        this.reload();
      });
  }
}
