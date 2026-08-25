import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { ExpensesFacade } from '../data/expenses.facade';
import { ClaimDetailComponent } from './claim-detail.component';

describe('ClaimDetailComponent', () => {
  let fixture: ComponentFixture<ClaimDetailComponent>;
  let facade: jest.Mocked<
    Pick<ExpensesFacade, 'loadClaimById' | 'uploadReceipt' | 'addLine' | 'submitClaim' | 'claim' | 'claimLoading' | 'claimError'>
  >;

  function setup(): void {
    TestBed.configureTestingModule({
      imports: [ClaimDetailComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ExpensesFacade, useValue: facade },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'claim-1' }) } },
        },
      ],
    });

    fixture = TestBed.createComponent(ClaimDetailComponent);
    fixture.detectChanges();
  }

  beforeEach(() => {
    facade = {
      loadClaimById: jest.fn(),
      uploadReceipt: jest.fn(),
      addLine: jest.fn(),
      submitClaim: jest.fn(),
      claim: signal(null),
      claimLoading: signal(false),
      claimError: signal(null),
    } as never;
  });

  it('should load the claim by the route id on init', () => {
    setup();

    expect(facade.loadClaimById).toHaveBeenCalledWith('claim-1');
  });

  it('onFilesSelected should pre-fill the line form from OCR suggestions but leave it editable', () => {
    facade.uploadReceipt.mockReturnValue(
      of({
        receiptReference: 'ref-1',
        suggestions: { vendor: 'Cafe Coffee Day', expenseDate: new Date('2026-01-15'), taxAmount: 12.5, amount: 250, confidence: 0.82 },
      } as never),
    );
    setup();

    fixture.componentInstance.onFilesSelected([new File(['x'], 'receipt.png')]);

    expect(facade.uploadReceipt).toHaveBeenCalledWith('claim-1', expect.any(File));
    expect(fixture.componentInstance.lineForm.controls.vendor.value).toBe('Cafe Coffee Day');
    expect(fixture.componentInstance.lineForm.controls.amount.value).toBe(250);
    expect(fixture.componentInstance.lineForm.controls.taxAmount.value).toBe(12.5);

    // The suggestion is a starting point, not the final word — the user can still change it.
    fixture.componentInstance.lineForm.controls.vendor.setValue('A Different Vendor');
    expect(fixture.componentInstance.lineForm.controls.vendor.value).toBe('A Different Vendor');
  });

  it('onFilesSelected should surface an error and not touch the form on failure', () => {
    const apiError: ApiError = { status: 400, code: 'ocr_failed', message: 'Could not read the receipt.' };
    facade.uploadReceipt.mockReturnValue(throwError(() => apiError));
    setup();

    fixture.componentInstance.onFilesSelected([new File(['x'], 'receipt.png')]);

    expect(fixture.componentInstance.uploadError()).toBe('Could not read the receipt.');
    expect(fixture.componentInstance.lineForm.controls.vendor.value).toBe('');
  });

  it('addLine should submit the edited form values including the uploaded receipt reference', () => {
    facade.uploadReceipt.mockReturnValue(of({ receiptReference: 'ref-1', suggestions: undefined } as never));
    facade.addLine.mockReturnValue(of(undefined));
    setup();

    fixture.componentInstance.onFilesSelected([new File(['x'], 'receipt.png')]);
    fixture.componentInstance.lineForm.setValue({
      category: 'Travel',
      amount: 500,
      currency: 0,
      expenseDate: new Date('2026-01-10'),
      vendor: 'Ola',
      taxAmount: null,
    });

    fixture.componentInstance.addLine();

    expect(facade.addLine).toHaveBeenCalledWith('claim-1', {
      category: 'Travel',
      amount: 500,
      currency: 0,
      expenseDate: new Date('2026-01-10'),
      vendor: 'Ola',
      taxAmount: undefined,
      receiptReference: 'ref-1',
    });
    expect(facade.loadClaimById).toHaveBeenCalledTimes(2);
  });

  it('addLine should not submit an invalid form', () => {
    setup();

    fixture.componentInstance.lineForm.controls.category.setValue('');
    fixture.componentInstance.addLine();

    expect(facade.addLine).not.toHaveBeenCalled();
    expect(fixture.componentInstance.lineForm.controls.category.touched).toBe(true);
  });

  it('submitClaim should surface returned warnings and reload', () => {
    facade.submitClaim.mockReturnValue(of({ warnings: ['Missing receipt for a line over the policy threshold.'] } as never));
    setup();

    fixture.componentInstance.submitClaim();

    expect(fixture.componentInstance.submitWarnings()).toEqual(['Missing receipt for a line over the policy threshold.']);
    expect(facade.loadClaimById).toHaveBeenCalledTimes(2);
  });

  it('submitClaim should surface a blocking policy error without reloading a second time', () => {
    const apiError: ApiError = { status: 400, code: 'expense_claim.policy_violation', message: 'Exceeds the policy cap.' };
    facade.submitClaim.mockReturnValue(throwError(() => apiError));
    setup();

    fixture.componentInstance.submitClaim();

    expect(fixture.componentInstance.submitError()).toBe('Exceeds the policy cap.');
    expect(facade.loadClaimById).toHaveBeenCalledTimes(1);
  });
});
