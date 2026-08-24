import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Currency } from '../../../core/models/currency';
import { AssetsService } from '../assets.service';
import { AssetStatus } from '../assets.models';
import { AssetList } from './asset-list';

describe('AssetList', () => {
  let assetsServiceSpy: jasmine.SpyObj<AssetsService>;

  beforeEach(async () => {
    assetsServiceSpy = jasmine.createSpyObj<AssetsService>('AssetsService', ['getAssets']);
    assetsServiceSpy.getAssets.and.returnValue(
      of({
        items: [
          {
            id: 'asset-1',
            assetTag: 'AST-1',
            category: 'Laptop',
            purchaseCost: 90000,
            purchaseCostCurrency: Currency.Inr,
            purchaseDate: '2026-01-01',
            serialNumber: 'SN-1',
            macAddress: null,
            warrantyExpiryDate: null,
            status: AssetStatus.InStock,
            depreciation: null,
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
      imports: [AssetList],
      providers: [provideRouter([]), { provide: AssetsService, useValue: assetsServiceSpy }],
    }).compileComponents();
  });

  it('loads and renders assets from the service on init', () => {
    const fixture = TestBed.createComponent(AssetList);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(assetsServiceSpy.getAssets).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    expect(component['assets']()).toEqual(jasmine.arrayWithExactContents([jasmine.objectContaining({ id: 'asset-1' })]));
    expect(component['totalCount']()).toBe(1);
  });

  it('re-requests the next page with the paginator page size', () => {
    const fixture = TestBed.createComponent(AssetList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.onPage({ pageIndex: 1, pageSize: 10, length: 1 });

    expect(assetsServiceSpy.getAssets).toHaveBeenCalledWith({ page: 2, pageSize: 10 });
  });
});
