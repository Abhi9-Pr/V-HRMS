import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { Currency } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { ExpenseClaimStatus } from '../expenses.models';
import { ClaimDetail } from './claim-detail';

describe('ClaimDetail', () => {
  let expensesServiceSpy: jasmine.SpyObj<ExpensesService>;

  const claim = {
    id: 'claim-1',
    status: ExpenseClaimStatus.Draft,
    settlementCurrency: Currency.Inr,
    total: 0,
    lines: [],
  };

  beforeEach(async () => {
    expensesServiceSpy = jasmine.createSpyObj<ExpensesService>('ExpensesService', [
      'getClaimById',
      'uploadReceipt',
      'addLine',
      'submitClaim',
    ]);
    expensesServiceSpy.getClaimById.and.returnValue(of(claim));
    expensesServiceSpy.addLine.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [ClaimDetail],
      providers: [
        { provide: ExpensesService, useValue: expensesServiceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'claim-1' }) } },
        },
      ],
    }).compileComponents();
  });

  it('loads the claim by id from the route on init', () => {
    const fixture = TestBed.createComponent(ClaimDetail);
    fixture.detectChanges();

    expect(expensesServiceSpy.getClaimById).toHaveBeenCalledWith('claim-1');
    expect(fixture.componentInstance['claim']()).toEqual(claim);
  });

  it('pre-fills the add-line form from OCR suggestions but leaves them editable', () => {
    expensesServiceSpy.uploadReceipt.and.returnValue(
      of({
        receiptReference: 'ref-1',
        suggestions: { vendor: 'Acme Co', expenseDate: '2026-01-15', taxAmount: 25, amount: 250, confidence: 0.8 },
      }),
    );

    const fixture = TestBed.createComponent(ClaimDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    const file = new File(['x'], 'receipt.png', { type: 'image/png' });
    const input = { files: [file] } as unknown as HTMLInputElement;
    component.onReceiptSelected({ target: input } as unknown as Event);

    const formValue = component['lineForm'].getRawValue();
    expect(formValue.vendor).toBe('Acme Co');
    expect(formValue.amount).toBe(250);
    expect(formValue.taxAmount).toBe(25);
    expect(component['receiptReference']()).toBe('ref-1');

    // Suggestions are defaults, not locked in — the user can still edit them.
    component['lineForm'].patchValue({ vendor: 'Corrected Vendor', amount: 999 });
    expect(component['lineForm'].getRawValue().vendor).toBe('Corrected Vendor');
    expect(component['lineForm'].getRawValue().amount).toBe(999);
  });

  it('submits the add-line form with the edited values and the receipt reference', () => {
    const fixture = TestBed.createComponent(ClaimDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component['lineForm'].setValue({
      category: 'Travel',
      amount: 500,
      currency: Currency.Inr,
      expenseDate: new Date(2026, 0, 10),
      vendor: 'Acme',
      taxAmount: 10,
    });

    component.addLine();

    expect(expensesServiceSpy.addLine).toHaveBeenCalledWith(
      'claim-1',
      jasmine.objectContaining({ category: 'Travel', amount: 500, vendor: 'Acme', taxAmount: 10, expenseDate: '2026-01-10' }),
    );
  });
});
