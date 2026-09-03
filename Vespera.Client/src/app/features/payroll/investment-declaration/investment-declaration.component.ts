import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, of } from 'rxjs';
import { EmptyStateComponent } from '../../../shared/states/empty-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { ApiError, InvestmentDeclarationDto, InvestmentDeclarationsClient } from 'vespera-shared';

function currentFinancialYear(): string {
  const now = new Date();
  const year = now.getMonth() >= 3 ? now.getFullYear() : now.getFullYear() - 1;
  return `${year}-${String((year + 1) % 100).padStart(2, '0')}`;
}

/** Employee self-service: submitting the first line creates the Draft declaration transparently
 * (see AddInvestmentDeclarationLineCommandHandler's get-or-create) — there's no separate "start a
 * declaration" step in this UI either. Proof upload is a plain storage-key text field for now; the
 * backend has no dedicated upload endpoint for declaration proofs yet. */
@Component({
  selector: 'vespera-investment-declaration',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatChipsModule,
    LoadingStateComponent,
    EmptyStateComponent,
  ],
  templateUrl: './investment-declaration.component.html',
})
export class InvestmentDeclarationComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly client = inject(InvestmentDeclarationsClient);

  readonly financialYear = currentFinancialYear();
  readonly declaration = signal<InvestmentDeclarationDto | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly lineForm = this.formBuilder.nonNullable.group({
    section: ['80C', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    proofFileReference: [''],
  });

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.loading.set(true);
    this.client
      .investmentDeclarations_GetMine(this.financialYear)
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.loading.set(false);
          return of(null);
        }),
      )
      .subscribe((declaration) => {
        this.declaration.set(declaration ?? null);
        this.loading.set(false);
      });
  }

  addLine(): void {
    if (this.lineForm.invalid || this.submitting()) {
      this.lineForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    const { section, amount, proofFileReference } = this.lineForm.getRawValue();

    this.client
      .investmentDeclarations_AddLine({
        financialYear: this.financialYear,
        // TODO: no "choose your tax regime" UI exists yet - there's no GetTaxRegimeVersionsQuery
        // for this screen to list from. A real declaration needs the employee's actual chosen
        // regime here; this is a placeholder until that query/picker exists.
        taxRegimeVersionId: '00000000-0000-0000-0000-000000000000',
        section,
        amount,
        proofFileReference: proofFileReference || undefined,
      })
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.submitting.set(false);
          return of(null);
        }),
      )
      .subscribe((id) => {
        if (id) {
          this.submitting.set(false);
          this.lineForm.reset({ section: '80C', amount: 0, proofFileReference: '' });
          this.reload();
        }
      });
  }

  submitDeclaration(): void {
    this.submitting.set(true);
    this.error.set(null);

    this.client
      .investmentDeclarations_Submit(this.financialYear)
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.submitting.set(false);
          return of(null);
        }),
      )
      .subscribe(() => {
        this.submitting.set(false);
        this.reload();
      });
  }
}
