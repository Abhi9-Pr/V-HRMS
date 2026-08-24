import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Currency } from '../../core/models/currency';
import { ExpensesService } from './expenses.service';

describe('ExpensesService', () => {
  let service: ExpensesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ExpensesService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExpensesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('openClaim posts to /api/v1/expenses/claims with an Idempotency-Key header', () => {
    service.openClaim({ settlementCurrency: Currency.Inr }).subscribe();

    const req = httpMock.expectOne('/api/v1/expenses/claims');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ settlementCurrency: Currency.Inr });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'claim-1' });
  });

  it('uploadReceipt posts multipart form data to the receipts endpoint', () => {
    const file = new File(['receipt-bytes'], 'receipt.png', { type: 'image/png' });
    service.uploadReceipt('claim-1', file).subscribe();

    const req = httpMock.expectOne('/api/v1/expenses/claims/claim-1/receipts');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBeTrue();
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({
      receiptReference: 'ref-1',
      suggestions: { vendor: 'Acme', expenseDate: '2026-01-10', taxAmount: 10, amount: 100, confidence: 0.75 },
    });
  });

  it('addLine posts the line payload to the claim', () => {
    service
      .addLine('claim-1', {
        category: 'Travel',
        amount: 1500,
        currency: Currency.Inr,
        expenseDate: '2026-01-10',
        receiptReference: 'ref-1',
        vendor: 'Acme',
        taxAmount: 10,
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/expenses/claims/claim-1/lines');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.category).toBe('Travel');
    expect(req.request.body.vendor).toBe('Acme');
    req.flush(null);
  });

  it('submitClaim posts to the submit endpoint', () => {
    service.submitClaim('claim-1').subscribe();

    const req = httpMock.expectOne('/api/v1/expenses/claims/claim-1/submit');
    expect(req.request.method).toBe('POST');
    req.flush({ warnings: [] });
  });

  it('decideApproval posts the decision payload', () => {
    service.decideApproval('claim-1', { approved: true, comment: 'looks good' }).subscribe();

    const req = httpMock.expectOne('/api/v1/expenses/claims/claim-1/decision');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ approved: true, comment: 'looks good' });
    req.flush(null);
  });

  it('settleClaim posts to the finance settlement endpoint', () => {
    service.settleClaim({ expenseClaimId: 'claim-1', payrollRunId: 'run-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/finance/expense-settlements');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ expenseClaimId: 'claim-1', payrollRunId: 'run-1' });
    req.flush(null);
  });

  it('getMyClaims sends paging as query params', () => {
    service.getMyClaims({ page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/expenses/claims');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: true });
  });
});
