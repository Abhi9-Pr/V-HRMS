import { createFeature, createReducer, on } from '@ngrx/store';
import { PayrollActions } from './payroll.actions';
import { initialPayrollState } from './payroll.state';

export const payrollFeature = createFeature({
  name: 'payroll',
  reducer: createReducer(
    initialPayrollState,

    on(PayrollActions.loadRuns, (state) => ({ ...state, runsLoading: true, error: null })),
    on(PayrollActions.loadRunsSuccess, (state, { items, totalCount }) => ({
      ...state,
      runs: items,
      runsTotalCount: totalCount,
      runsLoading: false,
    })),
    on(PayrollActions.loadRunsFailure, (state, { error }) => ({ ...state, runsLoading: false, error })),

    on(PayrollActions.openRun, (state) => ({ ...state, currentRunLoading: true, error: null })),
    on(PayrollActions.openRunFailure, (state, { error }) => ({ ...state, currentRunLoading: false, error })),

    on(PayrollActions.loadRun, (state) => ({ ...state, currentRunLoading: true, error: null })),
    on(PayrollActions.loadRunSuccess, (state, { run }) => ({ ...state, currentRun: run, currentRunLoading: false })),
    on(PayrollActions.loadRunFailure, (state, { error }) => ({ ...state, currentRunLoading: false, error })),

    on(
      PayrollActions.freezeAttendance,
      PayrollActions.runDryRun,
      PayrollActions.submitForReview,
      PayrollActions.approveRun,
      PayrollActions.finalizeRun,
      PayrollActions.publishRun,
      (state) => ({ ...state, currentRunLoading: true, error: null }),
    ),
    on(PayrollActions.runTransitionFailure, (state, { error }) => ({ ...state, currentRunLoading: false, error })),

    on(PayrollActions.loadVariance, (state) => ({ ...state, varianceLoading: true, error: null })),
    on(PayrollActions.loadVarianceSuccess, (state, { lines }) => ({ ...state, variance: lines, varianceLoading: false })),
    on(PayrollActions.loadVarianceFailure, (state, { error }) => ({ ...state, varianceLoading: false, error })),

    on(PayrollActions.clearCurrentRun, (state) => ({ ...state, currentRun: null, variance: [] })),
  ),
});

export const {
  name: payrollFeatureKey,
  reducer: payrollReducer,
  selectRuns,
  selectRunsTotalCount,
  selectRunsLoading,
  selectCurrentRun,
  selectCurrentRunLoading,
  selectVariance,
  selectVarianceLoading,
  selectError,
} = payrollFeature;
