import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  AssetRecoveriesClient,
  AssetsClient,
  LicensesClient,
  OffboardingChecklistsClient,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { AssetsFacade } from './assets.facade';

describe('AssetsFacade', () => {
  let assetsClient: jest.Mocked<
    Pick<
      AssetsClient,
      | 'assets_GetAssets'
      | 'assets_GetAssetById'
      | 'assets_CreateAsset'
      | 'assets_ConfigureDepreciation'
      | 'assets_AssignAsset'
      | 'assets_UploadHandoverSignature'
      | 'assets_RecordCondition'
      | 'assets_ReturnAsset'
      | 'assets_MarkUnderRepair'
      | 'assets_Retire'
    >
  >;
  let licensesClient: jest.Mocked<
    Pick<LicensesClient, 'licenses_GetLicenses' | 'licenses_GetUnusedSeatsReport' | 'licenses_CreateLicense' | 'licenses_AllocateSeat' | 'licenses_ReleaseSeat'>
  >;
  let recoveriesClient: jest.Mocked<
    Pick<
      AssetRecoveriesClient,
      | 'assetRecoveries_GetPending'
      | 'assetRecoveries_RecordCourierDispatch'
      | 'assetRecoveries_RecordReceived'
      | 'assetRecoveries_RecordDamageAssessment'
      | 'assetRecoveries_WriteOff'
      | 'assetRecoveries_Complete'
    >
  >;
  let checklistsClient: jest.Mocked<Pick<OffboardingChecklistsClient, 'offboardingChecklists_GetForEmployee' | 'offboardingChecklists_CompleteItem'>>;
  let facade: AssetsFacade;

  beforeEach(() => {
    assetsClient = {
      assets_GetAssets: jest.fn(),
      assets_GetAssetById: jest.fn(),
      assets_CreateAsset: jest.fn(),
      assets_ConfigureDepreciation: jest.fn(),
      assets_AssignAsset: jest.fn(),
      assets_UploadHandoverSignature: jest.fn(),
      assets_RecordCondition: jest.fn(),
      assets_ReturnAsset: jest.fn(),
      assets_MarkUnderRepair: jest.fn(),
      assets_Retire: jest.fn(),
    };
    licensesClient = {
      licenses_GetLicenses: jest.fn(),
      licenses_GetUnusedSeatsReport: jest.fn(),
      licenses_CreateLicense: jest.fn(),
      licenses_AllocateSeat: jest.fn(),
      licenses_ReleaseSeat: jest.fn(),
    };
    recoveriesClient = {
      assetRecoveries_GetPending: jest.fn(),
      assetRecoveries_RecordCourierDispatch: jest.fn(),
      assetRecoveries_RecordReceived: jest.fn(),
      assetRecoveries_RecordDamageAssessment: jest.fn(),
      assetRecoveries_WriteOff: jest.fn(),
      assetRecoveries_Complete: jest.fn(),
    };
    checklistsClient = {
      offboardingChecklists_GetForEmployee: jest.fn(),
      offboardingChecklists_CompleteItem: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        AssetsFacade,
        { provide: AssetsClient, useValue: assetsClient },
        { provide: LicensesClient, useValue: licensesClient },
        { provide: AssetRecoveriesClient, useValue: recoveriesClient },
        { provide: OffboardingChecklistsClient, useValue: checklistsClient },
      ],
    });

    facade = TestBed.inject(AssetsFacade);
  });

  it('loadAssets() should populate assets/totalCount on success', () => {
    assetsClient.assets_GetAssets.mockReturnValue(of({ items: [{ id: '1', assetTag: 'LAP-001' }], totalCount: 1 } as never));

    facade.loadAssets({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.assetsLoading()).toBe(false);
    expect(facade.assets()).toHaveLength(1);
    expect(facade.assetsTotalCount()).toBe(1);
    expect(assetsClient.assets_GetAssets).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadAssets() should populate error on failure and stop loading', () => {
    const apiError: ApiError = { status: 403, code: 'forbidden', message: 'no access' };
    assetsClient.assets_GetAssets.mockReturnValue(throwError(() => apiError));

    facade.loadAssets({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.assetsLoading()).toBe(false);
    expect(facade.assetsError()).toEqual(apiError);
  });

  it('loadAssetById() should populate asset on success', () => {
    assetsClient.assets_GetAssetById.mockReturnValue(of({ id: '1', assetTag: 'LAP-001' } as never));

    facade.loadAssetById('1');

    expect(facade.asset()).toEqual({ id: '1', assetTag: 'LAP-001' });
  });

  it('loadLicenses() should populate licenses/totalCount on success', () => {
    licensesClient.licenses_GetLicenses.mockReturnValue(of({ items: [{ id: 'lic-1' }], totalCount: 1 } as never));

    facade.loadLicenses({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.licenses()).toHaveLength(1);
    expect(facade.licensesTotalCount()).toBe(1);
  });

  it('loadUnusedSeatsReport() should populate the report rows', () => {
    licensesClient.licenses_GetUnusedSeatsReport.mockReturnValue(of([{ licenseId: 'lic-1', unusedSeats: 3 }] as never));

    facade.loadUnusedSeatsReport();

    expect(facade.unusedSeatsReport()).toHaveLength(1);
    expect(facade.unusedSeatsReportLoading()).toBe(false);
  });

  it('loadPendingRecoveries() should populate pendingRecoveries/totalCount on success', () => {
    recoveriesClient.assetRecoveries_GetPending.mockReturnValue(of({ items: [{ id: 'rec-1' }], totalCount: 1 } as never));

    facade.loadPendingRecoveries({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.pendingRecoveries()).toHaveLength(1);
    expect(facade.pendingRecoveriesTotalCount()).toBe(1);
  });

  it('loadOffboardingChecklist() should populate checklist on success', () => {
    checklistsClient.offboardingChecklists_GetForEmployee.mockReturnValue(of({ id: 'chk-1', items: [] } as never));

    facade.loadOffboardingChecklist('emp-1');

    expect(facade.checklist()).toEqual({ id: 'chk-1', items: [] });
    expect(checklistsClient.offboardingChecklists_GetForEmployee).toHaveBeenCalledWith('emp-1');
  });

  it('createAsset() should delegate to the generated client with a fresh idempotency key', (done) => {
    assetsClient.assets_CreateAsset.mockReturnValue(of({ id: 'asset-1' } as never));

    facade.createAsset({ assetTag: 'LAP-002' }).subscribe((result) => {
      expect(result.id).toBe('asset-1');
      expect(assetsClient.assets_CreateAsset).toHaveBeenCalledWith(expect.any(String), { assetTag: 'LAP-002' });
      done();
    });
  });

  it('configureDepreciation() should delegate to the generated client', (done) => {
    assetsClient.assets_ConfigureDepreciation.mockReturnValue(of(undefined));

    facade.configureDepreciation('asset-1', { usefulLifeMonths: 24 }).subscribe(() => {
      expect(assetsClient.assets_ConfigureDepreciation).toHaveBeenCalledWith('asset-1', expect.any(String), { usefulLifeMonths: 24 });
      done();
    });
  });

  it('assignAsset() should delegate to the generated client', (done) => {
    assetsClient.assets_AssignAsset.mockReturnValue(of({ id: 'assignment-1' } as never));

    facade.assignAsset('asset-1', { employeeId: 'emp-1' }).subscribe((result) => {
      expect(result.id).toBe('assignment-1');
      expect(assetsClient.assets_AssignAsset).toHaveBeenCalledWith('asset-1', expect.any(String), { employeeId: 'emp-1' });
      done();
    });
  });

  it('uploadHandoverSignature() should wrap the File into a FileParameter and delegate to the generated client', (done) => {
    assetsClient.assets_UploadHandoverSignature.mockReturnValue(of({ reference: 'sig-1' } as never));
    const file = new File(['x'], 'signature.png');

    facade.uploadHandoverSignature('assignment-1', file).subscribe((result) => {
      expect(result.reference).toBe('sig-1');
      expect(assetsClient.assets_UploadHandoverSignature).toHaveBeenCalledWith('assignment-1', expect.any(String), {
        data: file,
        fileName: 'signature.png',
      });
      done();
    });
  });

  it('recordCondition() should delegate to the generated client', (done) => {
    assetsClient.assets_RecordCondition.mockReturnValue(of(undefined));

    facade.recordCondition('assignment-1', { rating: 0, notes: 'Fine' }).subscribe(() => {
      expect(assetsClient.assets_RecordCondition).toHaveBeenCalledWith('assignment-1', expect.any(String), { rating: 0, notes: 'Fine' });
      done();
    });
  });

  it('returnAsset() should delegate to the generated client', (done) => {
    assetsClient.assets_ReturnAsset.mockReturnValue(of(undefined));

    facade.returnAsset('assignment-1', { condition: 'Good' }).subscribe(() => {
      expect(assetsClient.assets_ReturnAsset).toHaveBeenCalledWith('assignment-1', expect.any(String), { condition: 'Good' });
      done();
    });
  });

  it('markUnderRepair() should delegate to the generated client', (done) => {
    assetsClient.assets_MarkUnderRepair.mockReturnValue(of(undefined));

    facade.markUnderRepair('asset-1').subscribe(() => {
      expect(assetsClient.assets_MarkUnderRepair).toHaveBeenCalledWith('asset-1', expect.any(String));
      done();
    });
  });

  it('retireAsset() should delegate to the generated client', (done) => {
    assetsClient.assets_Retire.mockReturnValue(of(undefined));

    facade.retireAsset('asset-1').subscribe(() => {
      expect(assetsClient.assets_Retire).toHaveBeenCalledWith('asset-1', expect.any(String));
      done();
    });
  });

  it('createLicense() should delegate to the generated client', (done) => {
    licensesClient.licenses_CreateLicense.mockReturnValue(of({ id: 'lic-1' } as never));

    facade.createLicense({ productName: 'Figma', seatCount: 5 }).subscribe((result) => {
      expect(result.id).toBe('lic-1');
      expect(licensesClient.licenses_CreateLicense).toHaveBeenCalledWith(expect.any(String), { productName: 'Figma', seatCount: 5 });
      done();
    });
  });

  it('allocateSeat() should delegate to the generated client', (done) => {
    licensesClient.licenses_AllocateSeat.mockReturnValue(of({ id: 'alloc-1' } as never));

    facade.allocateSeat('lic-1', { employeeId: 'emp-1' }).subscribe((result) => {
      expect(result.id).toBe('alloc-1');
      expect(licensesClient.licenses_AllocateSeat).toHaveBeenCalledWith('lic-1', expect.any(String), { employeeId: 'emp-1' });
      done();
    });
  });

  it('releaseSeat() should delegate to the generated client', (done) => {
    licensesClient.licenses_ReleaseSeat.mockReturnValue(of(undefined));

    facade.releaseSeat('alloc-1').subscribe(() => {
      expect(licensesClient.licenses_ReleaseSeat).toHaveBeenCalledWith('alloc-1', expect.any(String));
      done();
    });
  });

  it('recordCourierDispatch() should delegate to the generated client', (done) => {
    recoveriesClient.assetRecoveries_RecordCourierDispatch.mockReturnValue(of(undefined));

    facade.recordCourierDispatch('rec-1', { carrier: 'FedEx', trackingReference: 'TRK-1' }).subscribe(() => {
      expect(recoveriesClient.assetRecoveries_RecordCourierDispatch).toHaveBeenCalledWith('rec-1', expect.any(String), {
        carrier: 'FedEx',
        trackingReference: 'TRK-1',
      });
      done();
    });
  });

  it('recordReceived() should delegate to the generated client', (done) => {
    recoveriesClient.assetRecoveries_RecordReceived.mockReturnValue(of(undefined));

    facade.recordReceived('rec-1').subscribe(() => {
      expect(recoveriesClient.assetRecoveries_RecordReceived).toHaveBeenCalledWith('rec-1', expect.any(String));
      done();
    });
  });

  it('recordDamageAssessment() should delegate to the generated client', (done) => {
    recoveriesClient.assetRecoveries_RecordDamageAssessment.mockReturnValue(of(undefined));

    facade.recordDamageAssessment('rec-1', { notes: 'Cracked screen' }).subscribe(() => {
      expect(recoveriesClient.assetRecoveries_RecordDamageAssessment).toHaveBeenCalledWith('rec-1', expect.any(String), { notes: 'Cracked screen' });
      done();
    });
  });

  it('writeOffRecovery() should delegate to the generated client', (done) => {
    recoveriesClient.assetRecoveries_WriteOff.mockReturnValue(of(undefined));

    facade.writeOffRecovery('rec-1', { amount: 500, currency: 0, reason: 'Unrepairable' }).subscribe(() => {
      expect(recoveriesClient.assetRecoveries_WriteOff).toHaveBeenCalledWith('rec-1', expect.any(String), {
        amount: 500,
        currency: 0,
        reason: 'Unrepairable',
      });
      done();
    });
  });

  it('completeRecovery() should delegate to the generated client', (done) => {
    recoveriesClient.assetRecoveries_Complete.mockReturnValue(of(undefined));

    facade.completeRecovery('rec-1').subscribe(() => {
      expect(recoveriesClient.assetRecoveries_Complete).toHaveBeenCalledWith('rec-1', expect.any(String));
      done();
    });
  });

  it('completeChecklistItem() should delegate to the generated client', (done) => {
    checklistsClient.offboardingChecklists_CompleteItem.mockReturnValue(of(undefined));

    facade.completeChecklistItem('chk-1', 0).subscribe(() => {
      expect(checklistsClient.offboardingChecklists_CompleteItem).toHaveBeenCalledWith('chk-1', 0, expect.any(String));
      done();
    });
  });
});
