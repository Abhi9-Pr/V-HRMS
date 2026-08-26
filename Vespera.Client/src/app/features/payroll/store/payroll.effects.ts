import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { PayrollClient } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { PayrollActions } from './payroll.actions';

export const loadRuns$ = createEffect(
  (actions$ = inject(Actions), client = inject(PayrollClient)) =>
    actions$.pipe(
      ofType(PayrollActions.loadRuns),
      switchMap(({ page, pageSize }) =>
        client.list8(page, pageSize, undefined, undefined).pipe(
          map((result) => PayrollActions.loadRunsSuccess({ items: result.items ?? [], totalCount: result.totalCount ?? 0 })),
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
        client.open({ month, year, idempotencyKey: undefined }).pipe(
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
        client.getById7(payrollRunId).pipe(
          map((run) => PayrollActions.loadRunSuccess({ run })),
          catchError((error: ApiError) => of(PayrollActions.loadRunFailure({ error }))),
        ),
      ),
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
        client.freezeAttendance(payrollRunId, { overrideReason }).pipe(
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
        client.runDryRun(payrollRunId).pipe(
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
        client.submitForReview(payrollRunId).pipe(
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
        client.approve(payrollRunId).pipe(
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
        client.finalize(payrollRunId, undefined).pipe(
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
        client.publish(payrollRunId).pipe(
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
        client.getVariance(payrollRunId).pipe(
          map((lines) => PayrollActions.loadVarianceSuccess({ lines })),
          catchError((error: ApiError) => of(PayrollActions.loadVarianceFailure({ error }))),
        ),
      ),
    ),
  { functional: true },
);
