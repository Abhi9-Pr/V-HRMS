import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { AssetsFacade } from '../data/assets.facade';
import { RecoveryDashboardComponent } from './recovery-dashboard.component';
import { AssetRecoveryDto, AssetRecoveryStatus, AuthService } from 'vespera-shared';

describe('RecoveryDashboardComponent', () => {
  let fixture: ComponentFixture<RecoveryDashboardComponent>;
  let facade: jest.Mocked<
    Pick<
      AssetsFacade,
      | 'loadPendingRecoveries'
      | 'pendingRecoveries'
      | 'pendingRecoveriesTotalCount'
      | 'pendingRecoveriesLoading'
      | 'pendingRecoveriesError'
    >
  >;

  function setup(recoveries: AssetRecoveryDto[]): void {
    facade = {
      loadPendingRecoveries: jest.fn(),
      pendingRecoveries: signal(recoveries),
      pendingRecoveriesTotalCount: signal(recoveries.length),
      pendingRecoveriesLoading: signal(false),
      pendingRecoveriesError: signal(null),
    } as never;

    TestBed.configureTestingModule({
      imports: [RecoveryDashboardComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AssetsFacade, useValue: facade },
        { provide: MatDialog, useValue: { open: jest.fn() } },
        { provide: AuthService, useValue: { permissions: signal(['Assets.Recover']) } },
      ],
    });

    fixture = TestBed.createComponent(RecoveryDashboardComponent);
    // Two ticks: ngAfterViewInit (fired by the first detectChanges) reassigns `columns` to attach
    // the actions cellTemplate, but that happens *after* the child DataTableComponent has already
    // been checked with the old value in the same pass — the updated columns (and therefore the
    // rendered action buttons) only reach the DOM on the following change-detection cycle.
    fixture.detectChanges();
    fixture.detectChanges();
  }

  it('should load pending recoveries on init', () => {
    setup([]);

    expect(facade.loadPendingRecoveries).toHaveBeenCalledWith({ page: 1, pageSize: 20, sortDescending: false });
  });

  it('a Pending recovery should only show the dispatch and mark-received actions', () => {
    setup([{ id: 'rec-1', assetId: 'asset-1', employeeId: 'emp-1', status: AssetRecoveryStatus._0 }]);

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((element) =>
      (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toEqual(expect.arrayContaining(['dispatch-action', 'receive-action']));
    expect(testIds).not.toContain('assess-damage-action');
    expect(testIds).not.toContain('write-off-action');
    expect(testIds).not.toContain('complete-action');
  });

  it('a Received recovery should show assess-damage, write-off, and complete but not dispatch', () => {
    setup([{ id: 'rec-2', assetId: 'asset-2', employeeId: 'emp-2', status: AssetRecoveryStatus._2 }]);

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((element) =>
      (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toEqual(expect.arrayContaining(['assess-damage-action', 'write-off-action', 'complete-action']));
    expect(testIds).not.toContain('dispatch-action');
  });

  it('canDispatch/canReceive/canAssessDamage/canWriteOff/canComplete should mirror AssetRecovery.cs guard conditions', () => {
    setup([]);
    const component = fixture.componentInstance;

    expect(component.canDispatch({ status: AssetRecoveryStatus._0 })).toBe(true);
    expect(component.canDispatch({ status: AssetRecoveryStatus._1 })).toBe(false);

    expect(component.canReceive({ status: AssetRecoveryStatus._0 })).toBe(true);
    expect(component.canReceive({ status: AssetRecoveryStatus._1 })).toBe(true);
    expect(component.canReceive({ status: AssetRecoveryStatus._2 })).toBe(false);

    expect(component.canWriteOff({ status: AssetRecoveryStatus._2 })).toBe(true);
    expect(component.canWriteOff({ status: AssetRecoveryStatus._3 })).toBe(true);
    expect(component.canWriteOff({ status: AssetRecoveryStatus._4 })).toBe(false);

    expect(component.canComplete({ status: AssetRecoveryStatus._4 })).toBe(true);
    expect(component.canComplete({ status: AssetRecoveryStatus._5 })).toBe(false);
  });
});
