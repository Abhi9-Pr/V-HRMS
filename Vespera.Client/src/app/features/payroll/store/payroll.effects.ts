import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { PayrollActions } from './payroll.actions';
import { ApiError, PayrollClient } from 'vespera-shared';

export const loadRuns$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.loadRuns),
      switchMap(({ page, pageSize }) =>
        client.payroll_List(page, pageSize, undefined, undefined).pipe(
          map((result) =>
            PayrollActions.loadRunsSuccess({ items: result.items ?? [], totalCount: result.totalCount ?? 0 }),
          ),
          catchError((error: ApiError) => of(PayrollActions.loadRunsFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const openRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.openRun),
      switchMap(({ month, year }) =>
        client.payroll_Open({ month, year, idempotencyKey: undefined }).pipe(
          map((payrollRunId) => PayrollActions.openRunSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.openRunFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const loadRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.loadRun),
      switchMap(({ payrollRunId }) =>
        client.payroll_GetById(payrollRunId).pipe(
          map((run) => PayrollActions.loadRunSuccess({ run })),
          catchError((error: ApiError) => of(PayrollActions.loadRunFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

// Nothing else populates `currentRun` after a fresh open — without this, the wizard's own
// `filter((run) => this.isNew() && run !== null)` subscription (its only navigation-to-the-new-run
// trigger) never fires, and the "open a run" form just sits there with the button re-enabled.
export const loadRunAfterOpen$ = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(PayrollActions.openRunSuccess),
      map(({ payrollRunId }) => PayrollActions.loadRun({ payrollRunId })),
    ),
  { functional: true },
);

// Every lifecycle-transition action shares the same shape: call the matching endpoint, then
// re-load the run so currentRun reflects its new Status — one effect per action rather than one
// generic "transition" effect, since each maps to a distinctly-named API method.
export const freezeAttendance$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.freezeAttendance),
      switchMap(({ payrollRunId, overrideReason }) =>
        client.payroll_FreezeAttendance(payrollRunId, { overrideReason }).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const runDryRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.runDryRun),
      switchMap(({ payrollRunId }) =>
        client.payroll_RunDryRun(payrollRunId).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const submitForReview$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.submitForReview),
      switchMap(({ payrollRunId }) =>
        client.payroll_SubmitForReview(payrollRunId).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const approveRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.approveRun),
      switchMap(({ payrollRunId }) =>
        client.payroll_Approve(payrollRunId).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const finalizeRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.finalizeRun),
      switchMap(({ payrollRunId }) =>
        client.payroll_Finalize(payrollRunId, undefined).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

export const publishRun$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.publishRun),
      switchMap(({ payrollRunId }) =>
        client.payroll_Publish(payrollRunId).pipe(
          map(() => PayrollActions.runTransitionSuccess({ payrollRunId })),
          catchError((error: ApiError) => of(PayrollActions.runTransitionFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);

// Re-fetches the run after every successful transition so the wizard's step indicator, the
// variance screen, and anything else reading `currentRun` all see the new Status without each
// having to know which action just ran.
export const reloadRunAfterTransition$ = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(PayrollActions.runTransitionSuccess),
      map(({ payrollRunId }) => PayrollActions.loadRun({ payrollRunId })),
    ),
  { functional: true },
);

export const loadVariance$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.loadVariance),
      switchMap(({ payrollRunId }) =>
        client.payroll_GetVariance(payrollRunId).pipe(
          map((lines) => PayrollActions.loadVarianceSuccess({ lines })),
          catchError((error: ApiError) => of(PayrollActions.loadVarianceFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);
