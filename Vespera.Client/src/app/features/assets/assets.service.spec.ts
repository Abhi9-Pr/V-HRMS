import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Currency } from '../../core/models/currency';
import { AssetsService } from './assets.service';
import { DepreciationMethod, AssetConditionRating, EmployeeExitReason } from './assets.models';

describe('AssetsService', () => {
  let service: AssetsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AssetsService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AssetsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createAsset posts to /api/v1/assets with an Idempotency-Key header', () => {
    service
      .createAsset({
        assetTag: 'AST-1',
        category: 'Laptop',
        purchaseCost: 90000,
        purchaseCostCurrency: Currency.Inr,
        purchaseDate: '2026-01-01',
        serialNumber: 'SN-1',
        macAddress: null,
        warrantyExpiryDate: null,
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/assets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.assetTag).toBe('AST-1');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'asset-1' });
  });

  it('configureDepreciation posts the schedule to the asset', () => {
    service
      .configureDepreciation('asset-1', {
        method: DepreciationMethod.StraightLine,
        usefulLifeMonths: 24,
        salvageValue: 20000,
        salvageValueCurrency: Currency.Inr,
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/assets/asset-1/depreciation');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.usefulLifeMonths).toBe(24);
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('assignAsset posts the employee id', () => {
    service.assignAsset('asset-1', { employeeId: 'emp-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/assets/asset-1/assign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ employeeId: 'emp-1' });
    req.flush({ id: 'assignment-1' });
  });

  it('uploadHandoverSignature posts multipart form data', () => {
    const file = new File(['signature-bytes'], 'sig.png', { type: 'image/png' });
    service.uploadHandoverSignature('assignment-1', file).subscribe();

    const req = httpMock.expectOne('/api/v1/assets/assignments/assignment-1/signature');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBeTrue();
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ reference: 'sig-ref-1' });
  });

  it('recordCondition posts rating and notes', () => {
    service.recordCondition('assignment-1', { rating: AssetConditionRating.Good, notes: 'fine' }).subscribe();

    const req = httpMock.expectOne('/api/v1/assets/assignments/assignment-1/condition-reports');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rating: AssetConditionRating.Good, notes: 'fine' });
    req.flush(null);
  });

  it('returnAsset posts the condition text', () => {
    service.returnAsset('assignment-1', { condition: 'Good condition' }).subscribe();

    const req = httpMock.expectOne('/api/v1/assets/assignments/assignment-1/return');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ condition: 'Good condition' });
    req.flush(null);
  });

  it('markUnderRepair and retireAsset post with no body', () => {
    service.markUnderRepair('asset-1').subscribe();
    const repairReq = httpMock.expectOne('/api/v1/assets/asset-1/under-repair');
    expect(repairReq.request.method).toBe('POST');
    repairReq.flush(null);

    service.retireAsset('asset-1').subscribe();
    const retireReq = httpMock.expectOne('/api/v1/assets/asset-1/retire');
    expect(retireReq.request.method).toBe('POST');
    retireReq.flush(null);
  });

  it('getAssets sends paging as query params', () => {
    service.getAssets({ page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/assets');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: true });
  });

  it('createLicense posts to /api/v1/licenses', () => {
    service.createLicense({ productName: 'Figma', seatCount: 5, expiresAt: null }).subscribe();

    const req = httpMock.expectOne('/api/v1/licenses');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'license-1' });
  });

  it('allocateSeat posts the employee id to the license', () => {
    service.allocateSeat('license-1', { employeeId: 'emp-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/licenses/license-1/allocations');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ employeeId: 'emp-1' });
    req.flush({ id: 'allocation-1' });
  });

  it('releaseSeat posts to the release endpoint', () => {
    service.releaseSeat('allocation-1').subscribe();

    const req = httpMock.expectOne('/api/v1/licenses/allocations/allocation-1/release');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('getUnusedSeatsReport gets the report with no params', () => {
    service.getUnusedSeatsReport().subscribe();

    const req = httpMock.expectOne('/api/v1/licenses/unused-seats-report');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('recordCourierDispatch posts carrier and tracking reference', () => {
    service.recordCourierDispatch('recovery-1', { carrier: 'DHL', trackingReference: 'TRK-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/asset-recoveries/recovery-1/courier-dispatch');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ carrier: 'DHL', trackingReference: 'TRK-1' });
    req.flush(null);
  });

  it('writeOffAsset posts amount, currency, and reason', () => {
    service.writeOffAsset('recovery-1', { amount: 500, currency: Currency.Inr, reason: 'Lost in transit' }).subscribe();

    const req = httpMock.expectOne('/api/v1/asset-recoveries/recovery-1/write-off');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ amount: 500, currency: Currency.Inr, reason: 'Lost in transit' });
    req.flush(null);
  });

  it('completeChecklistItem posts to the item completion endpoint', () => {
    service.completeChecklistItem('checklist-1', 0).subscribe();

    const req = httpMock.expectOne('/api/v1/offboarding-checklists/checklist-1/items/0/complete');
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('exitEmployee posts exit date and reason', () => {
    service.exitEmployee('emp-1', { exitDate: '2026-02-01', reason: EmployeeExitReason.Resignation }).subscribe();

    const req = httpMock.expectOne('/api/v1/employees/emp-1/exit');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ exitDate: '2026-02-01', reason: EmployeeExitReason.Resignation });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });
});
