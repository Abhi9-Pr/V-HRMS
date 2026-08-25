import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { AssetsFacade } from '../data/assets.facade';
import { AssetListComponent } from './asset-list.component';

describe('AssetListComponent', () => {
  let fixture: ComponentFixture<AssetListComponent>;
  let facade: jest.Mocked<Pick<AssetsFacade, 'loadAssets' | 'assets' | 'assetsTotalCount' | 'assetsLoading' | 'assetsError'>>;
  let dialog: jest.Mocked<Pick<MatDialog, 'open'>>;
  let router: jest.Mocked<Pick<Router, 'navigate'>>;

  beforeEach(async () => {
    facade = {
      loadAssets: jest.fn(),
      assets: signal([]),
      assetsTotalCount: signal(0),
      assetsLoading: signal(false),
      assetsError: signal(null),
    } as never;

    dialog = { open: jest.fn() };
    router = { navigate: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [AssetListComponent],
      providers: [
        { provide: AssetsFacade, useValue: facade },
        { provide: MatDialog, useValue: dialog },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: { permissions: signal(['Assets.Read', 'Assets.Write']) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetListComponent);
  });

  it('should load assets on init', () => {
    fixture.detectChanges();

    expect(facade.loadAssets).toHaveBeenCalledWith({ page: 1, pageSize: 20, sortDescending: false });
  });

  it('onQueryChange should reload with the new query', () => {
    fixture.detectChanges();

    fixture.componentInstance.onQueryChange({ page: 2, pageSize: 50, sortDescending: true });

    expect(facade.loadAssets).toHaveBeenLastCalledWith({ page: 2, pageSize: 50, sortDescending: true });
  });

  it('openCreateDialog should navigate to the new asset on close', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of('new-asset-id') } as never);
    fixture.detectChanges();

    fixture.componentInstance.openCreateDialog();

    expect(dialog.open).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/assets', 'new-asset-id']);
  });

  it('openCreateDialog should not navigate when the dialog is cancelled', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(null) } as never);
    fixture.detectChanges();

    fixture.componentInstance.openCreateDialog();

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('openAsset should navigate to the asset detail route', () => {
    fixture.detectChanges();

    fixture.componentInstance.openAsset({ id: 'asset-1' });

    expect(router.navigate).toHaveBeenCalledWith(['/assets', 'asset-1']);
  });
});
