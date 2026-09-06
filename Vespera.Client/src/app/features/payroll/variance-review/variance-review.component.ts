import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute } from '@angular/router';
import { Store } from '@ngrx/store';
import { EmptyStateComponent } from '../../../shared/states/empty-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { PayrollActions } from '../store/payroll.actions';
import { selectVariance, selectVarianceLoading } from '../store/payroll.reducer';

// Matches GetPayrollRunVarianceQueryHandler's PayrollVarianceFlag.ToString() values exactly.
const FLAG_LABELS: Record<string, string> = {
  NewJoiner: 'New joiner',
  Exit: 'Exit',
  LargeVariance: 'Large variance',
  ZeroNet: 'Zero net',
};

/** Flags any employee whose net moved more than the tenant's configured variance threshold versus
 * the prior finalized cycle, plus new joiners, exits, and zero-net anomalies — read straight from
 * `GetPayrollRunVarianceQuery`'s output, no client-side re-derivation of what counts as "large". */
@Component({
  selector: 'vespera-variance-review',
  imports: [DecimalPipe, MatTableModule, MatChipsModule, LoadingStateComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './variance-review.component.html',
})
export class VarianceReviewComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);

  readonly loading = toSignal(this.store.select(selectVarianceLoading), { initialValue: false });
  readonly lines = toSignal(this.store.select(selectVariance), { initialValue: [] });
  readonly displayedColumns = ['employeeId', 'priorNet', 'currentNet', 'variancePercent', 'flags'];

  readonly flagged = computed(() => this.lines().filter((line) => (line.flags?.length ?? 0) > 0));

  ngOnInit(): void {
    const payrollRunId = this.route.snapshot.paramMap.get('id');
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.loadVariance({ payrollRunId }));
    }
  }

  flagLabel(flag: string): string {
    return FLAG_LABELS[flag] ?? flag;
  }
}
