import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Currency } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { ExpenseClaimStatus } from '../expenses.models';
import { ClaimList } from './claim-list';

describe('ClaimList', () => {
  let expensesServiceSpy: jasmine.SpyObj<ExpensesService>;

  beforeEach(async () => {
    expensesServiceSpy = jasmine.createSpyObj<ExpensesService>('ExpensesService', ['getMyClaims', 'openClaim']);
    expensesServiceSpy.getMyClaims.and.returnValue(
      of({
        items: [
          {
            id: 'claim-1',
            status: ExpenseClaimStatus.Draft,
            settlementCurrency: Currency.Inr,
            total: 1500,
            lines: [],
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [ClaimList],
      providers: [provideRouter([]), { provide: ExpensesService, useValue: expensesServiceSpy }],
    }).compileComponents();
  });

  it('loads and renders claims from the service on init', () => {
    const fixture = TestBed.createComponent(ClaimList);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(expensesServiceSpy.getMyClaims).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    expect(component['claims']()).toEqual(jasmine.arrayWithExactContents([jasmine.objectContaining({ id: 'claim-1' })]));
    expect(component['totalCount']()).toBe(1);
  });

  it('re-requests the next page with the paginator page size', () => {
    const fixture = TestBed.createComponent(ClaimList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.onPage({ pageIndex: 1, pageSize: 10, length: 1 });

    expect(expensesServiceSpy.getMyClaims).toHaveBeenCalledWith({ page: 2, pageSize: 10 });
  });
});
