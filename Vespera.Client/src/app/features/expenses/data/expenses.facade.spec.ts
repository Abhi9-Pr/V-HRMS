import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  ExpensePoliciesClient,
  ExpenseSettlementsClient,
  ExpensesClient,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { ExpensesFacade } from './expenses.facade';

describe('ExpensesFacade', () => {
  let expensesClient: jest.Mocked<
    Pick<
      ExpensesClient,
      | 'expenses_GetMyClaims'
      | 'expenses_GetClaimById'
      | 'expenses_GetPendingApprovals'
      | 'expenses_OpenClaim'
      | 'expenses_UploadReceipt'
      | 'expenses_AddLine'
      | 'expenses_SubmitClaim'
      | 'expenses_DecideApproval'
    >
  >;
  let policiesClient: jest.Mocked<Pick<ExpensePoliciesClient, 'expensePolicies_Get' | 'expensePolicies_Create'>>;
  let settlementsClient: jest.Mocked<Pick<ExpenseSettlementsClient, 'expenseSettlements_Settle'>>;
  let facade: ExpensesFacade;

  beforeEach(() => {
    expensesClient = {
      expenses_GetMyClaims: jest.fn(),
      expenses_GetClaimById: jest.fn(),
      expenses_GetPendingApprovals: jest.fn(),
      expenses_OpenClaim: jest.fn(),
      expenses_UploadReceipt: jest.fn(),
      expenses_AddLine: jest.fn(),
      expenses_SubmitClaim: jest.fn(),
      expenses_DecideApproval: jest.fn(),
    };
    policiesClient = { expensePolicies_Get: jest.fn(), expensePolicies_Create: jest.fn() };
    settlementsClient = { expenseSettlements_Settle: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        ExpensesFacade,
        { provide: ExpensesClient, useValue: expensesClient },
        { provide: ExpensePoliciesClient, useValue: policiesClient },
        { provide: ExpenseSettlementsClient, useValue: settlementsClient },
      ],
    });

    facade = TestBed.inject(ExpensesFacade);
  });

  it('loadMyClaims() should populate claims/totalCount on success', () => {
    expensesClient.expenses_GetMyClaims.mockReturnValue(
      of({ items: [{ id: '1', status: 0 }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadMyClaims({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.claimsLoading()).toBe(false);
    expect(facade.claimsError()).toBeNull();
    expect(facade.claims()).toHaveLength(1);
    expect(facade.claimsTotalCount()).toBe(1);
    expect(expensesClient.expenses_GetMyClaims).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadMyClaims() should populate error on failure and stop loading', () => {
    const apiError: ApiError = { status: 500, code: 'server_error', message: 'boom' };
    expensesClient.expenses_GetMyClaims.mockReturnValue(throwError(() => apiError));

    facade.loadMyClaims({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.claimsLoading()).toBe(false);
    expect(facade.claimsError()).toEqual(apiError);
    expect(facade.claims()).toHaveLength(0);
  });

  it('loadClaimById() should populate claim on success', () => {
    expensesClient.expenses_GetClaimById.mockReturnValue(of({ id: '1', status: 0 } as never));

    facade.loadClaimById('1');

    expect(facade.claimLoading()).toBe(false);
    expect(facade.claim()?.id).toBe('1');
    expect(expensesClient.expenses_GetClaimById).toHaveBeenCalledWith('1');
  });

  it('loadPendingApprovals() should populate pendingApprovals/totalCount on success', () => {
    expensesClient.expenses_GetPendingApprovals.mockReturnValue(
      of({ items: [{ id: '2', status: 1 }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadPendingApprovals({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.pendingApprovals()).toHaveLength(1);
    expect(facade.pendingApprovalsTotalCount()).toBe(1);
    expect(expensesClient.expenses_GetPendingApprovals).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadPolicies() should populate policies/totalCount on success', () => {
    policiesClient.expensePolicies_Get.mockReturnValue(
      of({ items: [{ id: 'p1', category: 'Travel' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadPolicies({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.policies()).toHaveLength(1);
    expect(facade.policiesTotalCount()).toBe(1);
    expect(policiesClient.expensePolicies_Get).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('openClaim() should delegate to the generated client with a fresh idempotency key', (done) => {
    expensesClient.expenses_OpenClaim.mockReturnValue(of({ id: 'claim-1' } as never));

    facade.openClaim({ settlementCurrency: 0 }).subscribe((result) => {
      expect(result.id).toBe('claim-1');
      const [idempotencyKey, body] = expensesClient.expenses_OpenClaim.mock.calls[0];
      expect(typeof idempotencyKey).toBe('string');
      expect(idempotencyKey!.length).toBeGreaterThan(0);
      expect(body).toEqual({ settlementCurrency: 0 });
      done();
    });
  });

  it('uploadReceipt() should wrap the File into a FileParameter and delegate to the generated client', (done) => {
    expensesClient.expenses_UploadReceipt.mockReturnValue(of({ receiptReference: 'ref-1' } as never));
    const file = new File(['x'], 'receipt.png', { type: 'image/png' });

    facade.uploadReceipt('claim-1', file).subscribe((result) => {
      expect(result.receiptReference).toBe('ref-1');
      const [claimId, idempotencyKey, fileParameter] = expensesClient.expenses_UploadReceipt.mock.calls[0];
      expect(claimId).toBe('claim-1');
      expect(typeof idempotencyKey).toBe('string');
      expect(fileParameter).toEqual({ data: file, fileName: 'receipt.png' });
      done();
    });
  });

  it('addLine() should delegate to the generated client', (done) => {
    expensesClient.expenses_AddLine.mockReturnValue(of(undefined));
    const request = { category: 'Travel', amount: 100, currency: 0, expenseDate: new Date('2026-01-01') };

    facade.addLine('claim-1', request).subscribe(() => {
      const [claimId, , body] = expensesClient.expenses_AddLine.mock.calls[0];
      expect(claimId).toBe('claim-1');
      expect(body).toEqual(request);
      done();
    });
  });

  it('submitClaim() should delegate to the generated client', (done) => {
    expensesClient.expenses_SubmitClaim.mockReturnValue(of({ warnings: ['over threshold'] } as never));

    facade.submitClaim('claim-1').subscribe((result) => {
      expect(result.warnings).toEqual(['over threshold']);
      expect(expensesClient.expenses_SubmitClaim).toHaveBeenCalledWith('claim-1', expect.any(String));
      done();
    });
  });

  it('decideApproval() should delegate to the generated client', (done) => {
    expensesClient.expenses_DecideApproval.mockReturnValue(of(undefined));

    facade.decideApproval('claim-1', { approved: true, comment: 'ok' }).subscribe(() => {
      const [claimId, , body] = expensesClient.expenses_DecideApproval.mock.calls[0];
      expect(claimId).toBe('claim-1');
      expect(body).toEqual({ approved: true, comment: 'ok' });
      done();
    });
  });

  it('createPolicy() should delegate to the generated client', (done) => {
    policiesClient.expensePolicies_Create.mockReturnValue(of({ id: 'p1' } as never));

    facade.createPolicy({ category: 'Travel', maxAmountPerClaim: 5000 }).subscribe((result) => {
      expect(result.id).toBe('p1');
      done();
    });
  });

  it('settleClaim() should delegate to the generated client', (done) => {
    settlementsClient.expenseSettlements_Settle.mockReturnValue(of(undefined));

    facade.settleClaim({ expenseClaimId: 'claim-1', payrollRunId: 'run-1' }).subscribe(() => {
      const [, body] = settlementsClient.expenseSettlements_Settle.mock.calls[0];
      expect(body).toEqual({ expenseClaimId: 'claim-1', payrollRunId: 'run-1' });
      done();
    });
  });
});
