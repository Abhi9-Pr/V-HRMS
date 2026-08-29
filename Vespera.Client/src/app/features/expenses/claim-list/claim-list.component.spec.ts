import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { ExpensesFacade } from '../data/expenses.facade';
import { ClaimListComponent } from './claim-list.component';
import { AuthService } from 'vespera-shared';

describe('ClaimListComponent', () => {
  let fixture: ComponentFixture<ClaimListComponent>;
  let facade: jest.Mocked<
    Pick<ExpensesFacade, 'loadMyClaims' | 'claims' | 'claimsTotalCount' | 'claimsLoading' | 'claimsError'>
  >;
  let dialog: jest.Mocked<Pick<MatDialog, 'open'>>;
  let router: jest.Mocked<Pick<Router, 'navigate'>>;

  beforeEach(async () => {
    facade = {
      loadMyClaims: jest.fn(),
      claims: signal([]),
      claimsTotalCount: signal(0),
      claimsLoading: signal(false),
      claimsError: signal(null),
    } as never;

    dialog = { open: jest.fn() };
    router = { navigate: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [ClaimListComponent],
      providers: [
        { provide: ExpensesFacade, useValue: facade },
        { provide: MatDialog, useValue: dialog },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: { permissions: signal(['Expenses.Submit', 'Expenses.Approve']) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimListComponent);
  });

  it('should load claims on init', () => {
    fixture.detectChanges();

    expect(facade.loadMyClaims).toHaveBeenCalledWith({ page: 1, pageSize: 20, sortDescending: false });
  });

  it('openCreateDialog should navigate to the new claim on close', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of('new-claim-id') } as never);
    fixture.detectChanges();

    fixture.componentInstance.openCreateDialog();

    expect(dialog.open).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/expenses', 'new-claim-id']);
  });

  it('openCreateDialog should not navigate when the dialog is cancelled', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(null) } as never);
    fixture.detectChanges();

    fixture.componentInstance.openCreateDialog();

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('openClaim should navigate to the claim detail route', () => {
    fixture.detectChanges();

    fixture.componentInstance.openClaim({ id: 'claim-1' });

    expect(router.navigate).toHaveBeenCalledWith(['/expenses', 'claim-1']);
  });
});
