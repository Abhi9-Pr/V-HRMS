import { PayrollRunDto, PayrollRunSummaryDto, PayrollVarianceLine } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';

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
