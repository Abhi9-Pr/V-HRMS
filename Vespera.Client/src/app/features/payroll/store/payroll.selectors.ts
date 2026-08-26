import { createSelector } from '@ngrx/store';
import { selectCurrentRun } from './payroll.reducer';

/** Zero-based step index for `MatStepper`, derived from `PayrollRunStatus` — the wizard reads this
 * instead of re-deriving the mapping itself, so the state-machine order lives in one place. */
export const selectCurrentRunStepIndex = createSelector(selectCurrentRun, (run): number => {
  switch (run?.status) {
    case 'Draft':
      return 0;
    case 'AttendanceFrozen':
      return 1;
    case 'DryRun':
      return 2;
    case 'Review':
      return 3;
    case 'Approved':
      return 4;
    case 'Finalized':
      return 5;
    case 'Published':
      return 6;
    default:
      return 0;
  }
});

export const selectCanFreezeAttendance = createSelector(selectCurrentRun, (run) => run?.status === 'Draft');
export const selectCanRunDryRun = createSelector(
  selectCurrentRun,
  (run) => run?.status === 'AttendanceFrozen' || run?.status === 'DryRun' || run?.status === 'Review',
);
export const selectCanSubmitForReview = createSelector(selectCurrentRun, (run) => run?.status === 'DryRun');
export const selectCanApprove = createSelector(selectCurrentRun, (run) => run?.status === 'Review');
export const selectCanFinalize = createSelector(selectCurrentRun, (run) => run?.status === 'Approved');
export const selectCanPublish = createSelector(selectCurrentRun, (run) => run?.status === 'Finalized');
