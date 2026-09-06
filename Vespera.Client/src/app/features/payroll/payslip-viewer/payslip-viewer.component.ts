import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ActivatedRoute } from '@angular/router';
import { Store } from '@ngrx/store';
import { catchError, of } from 'rxjs';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { PayrollActions } from '../store/payroll.actions';
import { selectCurrentRun, selectCurrentRunLoading } from '../store/payroll.reducer';
import { ApiError, PayslipsClient } from 'vespera-shared';

/**
 * Only the single-period Gross/Deductions/Net breakdown is real chart data — it comes straight off
 * the already-loaded PayrollRun line for this employee. "Net pay trend" and "YTD" (which the brief
 * also asks for) would need a payslip-history query across multiple runs that doesn't exist yet;
 * this screen deliberately doesn't fake that data with a single point pretending to be a trend.
 */
@Component({
  selector: 'vespera-payslip-viewer',
  imports: [MatButtonModule, NgxChartsModule, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './payslip-viewer.component.html',
})
export class PayslipViewerComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly client = inject(PayslipsClient);

  readonly run = toSignal(this.store.select(selectCurrentRun), { initialValue: null });
  readonly loading = toSignal(this.store.select(selectCurrentRunLoading), { initialValue: false });

  readonly employeeId = signal<string | null>(null);
  readonly payslipId = signal<string | null>(null);
  readonly downloadUrl = signal<string | null>(null);
  readonly generating = signal(false);
  readonly error = signal<string | null>(null);

  readonly line = computed(() => this.run()?.lines?.find((l) => l.employeeId === this.employeeId()) ?? null);

  readonly breakdownChart = computed(() => {
    const line = this.line();
    if (!line) {
      return [];
    }

    return [
      { name: 'Net Pay', value: line.net ?? 0 },
      { name: 'Deductions', value: line.deductions ?? 0 },
    ];
  });

  ngOnInit(): void {
    const payrollRunId = this.route.snapshot.paramMap.get('id');
    const employeeId = this.route.snapshot.paramMap.get('employeeId');
    this.employeeId.set(employeeId);

    if (payrollRunId) {
      this.store.dispatch(PayrollActions.loadRun({ payrollRunId }));
    }
  }

  generate(): void {
    const payrollRunId = this.run()?.id;
    const employeeId = this.employeeId();
    if (!payrollRunId || !employeeId || this.generating()) {
      return;
    }

    this.generating.set(true);
    this.error.set(null);

    this.client
      .payslips_Generate({ payrollRunId, employeeId })
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.generating.set(false);
          return of(null);
        }),
      )
      .subscribe((id) => {
        if (id) {
          this.payslipId.set(id);
          this.generating.set(false);
        }
      });
  }

  loadDownloadUrl(): void {
    const payslipId = this.payslipId();
    if (!payslipId) {
      return;
    }

    this.client
      .payslips_GetDownloadUrl(payslipId)
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          return of(null);
        }),
      )
      .subscribe((url) => this.downloadUrl.set(url ?? null));
  }
}
