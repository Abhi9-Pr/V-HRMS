import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AssetsService } from '../assets.service';
import { AssetRecoveryStatus } from '../assets.models';
import { RecoveryDashboard } from './recovery-dashboard';

describe('RecoveryDashboard', () => {
  let assetsServiceSpy: jasmine.SpyObj<AssetsService>;

  beforeEach(async () => {
    assetsServiceSpy = jasmine.createSpyObj<AssetsService>('AssetsService', ['getPendingRecoveries']);
    assetsServiceSpy.getPendingRecoveries.and.returnValue(
      of({
        items: [
          {
            id: 'recovery-pending',
            assetAssignmentId: 'assignment-1',
            assetId: 'asset-1',
            employeeId: 'emp-1',
            initiatedAt: '2026-01-01T00:00:00Z',
            status: AssetRecoveryStatus.Pending,
            courierCarrier: null,
            courierTrackingReference: null,
            damageAssessmentNotes: null,
            writeOffAmount: null,
            writeOffAmountCurrency: null,
            writeOffReason: null,
          },
          {
            id: 'recovery-received',
            assetAssignmentId: 'assignment-2',
            assetId: 'asset-2',
            employeeId: 'emp-2',
            initiatedAt: '2026-01-01T00:00:00Z',
            status: AssetRecoveryStatus.Received,
            courierCarrier: 'DHL',
            courierTrackingReference: 'TRK-1',
            damageAssessmentNotes: null,
            writeOffAmount: null,
            writeOffAmountCurrency: null,
            writeOffReason: null,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [RecoveryDashboard],
      providers: [provideRouter([]), { provide: AssetsService, useValue: assetsServiceSpy }],
    }).compileComponents();
  });

  it('shows only Courier Dispatch and Mark Received actions for a Pending recovery', () => {
    const fixture = TestBed.createComponent(RecoveryDashboard);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.canDispatch(AssetRecoveryStatus.Pending)).toBeTrue();
    expect(component.canReceive(AssetRecoveryStatus.Pending)).toBeTrue();
    expect(component.canAssessDamage(AssetRecoveryStatus.Pending)).toBeFalse();
    expect(component.canWriteOff(AssetRecoveryStatus.Pending)).toBeFalse();
    expect(component.canComplete(AssetRecoveryStatus.Pending)).toBeFalse();

    const rows = fixture.nativeElement.querySelectorAll('tr[mat-row]');
    const pendingRowActions = rows[0].querySelectorAll('button');
    const pendingActionTestIds = Array.from(pendingRowActions).map((b: any) => b.getAttribute('data-testid'));
    expect(pendingActionTestIds).toContain('action-dispatch');
    expect(pendingActionTestIds).toContain('action-receive');
    expect(pendingActionTestIds).not.toContain('action-damage');
    expect(pendingActionTestIds).not.toContain('action-write-off');
    expect(pendingActionTestIds).not.toContain('action-complete');
  });

  it('shows Damage Assessment, Write Off, and Complete actions for a Received recovery, but not Dispatch', () => {
    const fixture = TestBed.createComponent(RecoveryDashboard);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.canDispatch(AssetRecoveryStatus.Received)).toBeFalse();
    expect(component.canReceive(AssetRecoveryStatus.Received)).toBeFalse();
    expect(component.canAssessDamage(AssetRecoveryStatus.Received)).toBeTrue();
    expect(component.canWriteOff(AssetRecoveryStatus.Received)).toBeTrue();
    expect(component.canComplete(AssetRecoveryStatus.Received)).toBeTrue();

    const rows = fixture.nativeElement.querySelectorAll('tr[mat-row]');
    const receivedRowActions = rows[1].querySelectorAll('button');
    const receivedActionTestIds = Array.from(receivedRowActions).map((b: any) => b.getAttribute('data-testid'));
    expect(receivedActionTestIds).not.toContain('action-dispatch');
    expect(receivedActionTestIds).not.toContain('action-receive');
    expect(receivedActionTestIds).toContain('action-damage');
    expect(receivedActionTestIds).toContain('action-write-off');
    expect(receivedActionTestIds).toContain('action-complete');
  });
});
