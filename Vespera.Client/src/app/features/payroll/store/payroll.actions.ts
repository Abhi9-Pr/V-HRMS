import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { PayrollRunDto, PayrollRunSummaryDto, PayrollVarianceLine } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';

/**
 * The payroll run's lifecycle, in one action group: this is the multi-step, multi-screen state
 * docs/frontend-state.md reserves NgRx for — the wizard, the variance-review screen, and (once a
 * run is loaded) the payslip viewer all read `currentRun` from the same slice rather than each
 * re-fetching it.
 */
export const PayrollActions = createActionGroup({
  source: 'Payroll',
  events: {
    'Load Runs': props<{ page: number; pageSize: number }>(),
    'Load Runs Success': props<{ items: PayrollRunSummaryDto[]; totalCount: number }>(),
    'Load Runs Failure': props<{ error: ApiError }>(),

    'Open Run': props<{ month: number; year: number }>(),
    'Open Run Success': props<{ payrollRunId: string }>(),
    'Open Run Failure': props<{ error: ApiError }>(),

    'Load Run': props<{ payrollRunId: string }>(),
    'Load Run Success': props<{ run: PayrollRunDto }>(),
    'Load Run Failure': props<{ error: ApiError }>(),

    'Freeze Attendance': props<{ payrollRunId: string; overrideReason?: string }>(),
    'Run Dry Run': props<{ payrollRunId: string }>(),
    'Submit For Review': props<{ payrollRunId: string }>(),
    'Approve Run': props<{ payrollRunId: string }>(),
    'Finalize Run': props<{ payrollRunId: string }>(),
    'Publish Run': props<{ payrollRunId: string }>(),
    'Run Transition Success': props<{ payrollRunId: string }>(),
    'Run Transition Failure': props<{ error: ApiError }>(),

    'Load Variance': props<{ payrollRunId: string }>(),
    'Load Variance Success': props<{ lines: PayrollVarianceLine[] }>(),
    'Load Variance Failure': props<{ error: ApiError }>(),

    'Clear Current Run': emptyProps(),
  },
});
