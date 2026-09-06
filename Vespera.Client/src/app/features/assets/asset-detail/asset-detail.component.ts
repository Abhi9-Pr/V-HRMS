import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { HasPermissionDirective } from '../../../core/directives/has-permission.directive';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { CurrencyDisplayPipe } from '../../../shared/pipes/currency-display.pipe';
import { TimezoneDatePipe } from '../../../shared/pipes/timezone-date.pipe';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { AssetsFacade } from '../data/assets.facade';
import { ASSET_STATUS_LABELS, CURRENCY_LABELS, DEPRECIATION_METHOD_LABELS } from '../assets.labels';
import { ApiError, AssetStatus, Currency, DepreciationMethod, Permissions } from 'vespera-shared';

@Component({
    selector: 'vespera-asset-detail',
    imports: [
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatButtonModule,
        FormErrorComponent,
        PermissionButtonComponent,
        HasPermissionDirective,
        ErrorStateComponent,
        LoadingStateComponent,
        CurrencyDisplayPipe,
        TimezoneDatePipe,
    ],
    templateUrl: './asset-detail.component.html'
})
export class AssetDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);

  readonly Permissions = Permissions;
  readonly AssetStatus = AssetStatus;
  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly depreciationMethods = Object.values(DepreciationMethod).filter(
    (value): value is DepreciationMethod => typeof value === 'number',
  );
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];
  readonly statusLabel = (status: AssetStatus): string => ASSET_STATUS_LABELS[status];
  readonly depreciationMethodLabel = (method: DepreciationMethod): string => DEPRECIATION_METHOD_LABELS[method];

  readonly asset = this.assetsFacade.asset;
  readonly loading = this.assetsFacade.assetLoading;
  readonly error = this.assetsFacade.assetError;

  readonly depreciationSaving = signal(false);
  readonly depreciationError = signal<string | null>(null);

  readonly assigning = signal(false);
  readonly assignError = signal<string | null>(null);

  readonly actionInFlight = signal(false);
  readonly actionError = signal<string | null>(null);

  private assetId = '';

  readonly depreciationForm = this.formBuilder.nonNullable.group({
    method: [DepreciationMethod._0, [Validators.required]],
    usefulLifeMonths: [36, [Validators.required, Validators.min(1)]],
    salvageValue: [0, [Validators.required, Validators.min(0)]],
    salvageValueCurrency: [Currency._0, [Validators.required]],
  });

  readonly assignForm = this.formBuilder.nonNullable.group({
    employeeId: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.assetId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.assetsFacade.loadAssetById(this.assetId);
  }

  configureDepreciation(): void {
    if (this.depreciationForm.invalid || this.depreciationSaving()) {
      this.depreciationForm.markAllAsTouched();
      return;
    }

    this.depreciationSaving.set(true);
    this.depreciationError.set(null);

    this.assetsFacade
      .configureDepreciation(this.assetId, this.depreciationForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.depreciationError.set(apiError.message);
          this.depreciationSaving.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.depreciationSaving.set(false);
        this.reload();
      });
  }

  assign(): void {
    if (this.assignForm.invalid || this.assigning()) {
      this.assignForm.markAllAsTouched();
      return;
    }

    this.assigning.set(true);
    this.assignError.set(null);

    this.assetsFacade
      .assignAsset(this.assetId, this.assignForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.assignError.set(apiError.message);
          this.assigning.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.assigning.set(false);
        void this.router.navigate(['/assets', this.assetId, 'handover', result.id]);
      });
  }

  markUnderRepair(): void {
    this.runAction(this.assetsFacade.markUnderRepair(this.assetId));
  }

  retire(): void {
    this.runAction(this.assetsFacade.retireAsset(this.assetId));
  }

  private runAction(action$: ReturnType<AssetsFacade['markUnderRepair']>): void {
    if (this.actionInFlight()) {
      return;
    }

    this.actionInFlight.set(true);
    this.actionError.set(null);

    action$
      .pipe(
        catchError((apiError: ApiError) => {
          this.actionError.set(apiError.message);
          this.actionInFlight.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.actionInFlight.set(false);
        this.reload();
      });
  }
}
