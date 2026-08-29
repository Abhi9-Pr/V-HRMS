import { ApiError, PayrollRunDto, PayrollRunSummaryDto, PayrollVarianceLine } from 'vespera-shared';

export interface PayrollState {
  runs: PayrollRunSummaryDto[];
  runsTotalCount: number;
  runsLoading: boolean;
  currentRun: PayrollRunDto | null;
  currentRunLoading: boolean;
  variance: PayrollVarianceLine[];
  varianceLoading: boolean;
  error: ApiError | null;
}

export const initialPayrollState: PayrollState = {
  runs: [],
  runsTotalCount: 0,
  runsLoading: false,
  currentRun: null,
  currentRunLoading: false,
  variance: [],
  varianceLoading: false,
  error: null,
};
