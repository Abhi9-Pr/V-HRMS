import { Component, OnInit, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatStepperModule } from '@angular/material/stepper';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { filter } from 'rxjs';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { PayrollActions } from '../store/payroll.actions';
import {
  selectCanApprove,
  selectCanFinalize,
  selectCanFreezeAttendance,
  selectCanPublish,
  selectCanRunDryRun,
  selectCanSubmitForReview,
  selectCurrentRunStepIndex,
} from '../store/payroll.selectors';
import { selectCurrentRun, selectCurrentRunLoading, selectError } from '../store/payroll.reducer';

/**
 * Mirrors PayrollRun's own state machine one step at a time — each step's action dispatches an
 * NgRx action, the effect calls the matching endpoint and reloads the run, and the stepper's
 * selected index follows `selectCurrentRunStepIndex` rather than being tracked separately, so the
 * wizard can never show a step that disagrees with what the server actually did.
 */
@Component({
    selector: 'vespera-payroll-run-wizard',
    imports: [
        ReactiveFormsModule,
        FormsModule,
        RouterLink,
        MatStepperModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        LoadingStateComponent,
    ],
    templateUrl: './payroll-run-wizard.component.html'
})
export class PayrollRunWizardComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly run = toSignal(this.store.select(selectCurrentRun), { initialValue: null });
  readonly loading = toSignal(this.store.select(selectCurrentRunLoading), { initialValue: false });
  readonly error = toSignal(this.store.select(selectError), { initialValue: null });
  readonly stepIndex = toSignal(this.store.select(selectCurrentRunStepIndex), { initialValue: 0 });
  readonly canFreezeAttendance = toSignal(this.store.select(selectCanFreezeAttendance), { initialValue: false });
  readonly canRunDryRun = toSignal(this.store.select(selectCanRunDryRun), { initialValue: false });
  readonly canSubmitForReview = toSignal(this.store.select(selectCanSubmitForReview), { initialValue: false });
  readonly canApprove = toSignal(this.store.select(selectCanApprove), { initialValue: false });
  readonly canFinalize = toSignal(this.store.select(selectCanFinalize), { initialValue: false });
  readonly canPublish = toSignal(this.store.select(selectCanPublish), { initialValue: false });

  readonly isNew = signal(true);
  readonly overrideReason = signal('');

  readonly openForm = this.formBuilder.nonNullable.group({
    month: [new Date().getMonth() + 1, [Validators.required, Validators.min(1), Validators.max(12)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(2000)]],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isNew.set(false);
      this.store.dispatch(PayrollActions.loadRun({ payrollRunId: id }));
      this.store.dispatch(PayrollActions.loadVariance({ payrollRunId: id }));
    } else {
      this.store.dispatch(PayrollActions.clearCurrentRun());
    }

    // Once a new run is opened, navigate to its own /payroll/runs/:id so a refresh (or sharing
    // the link) lands back on the same run instead of the "create new" form.
    this.store
      .select(selectCurrentRun)
      .pipe(filter((run) => this.isNew() && run !== null))
      .subscribe((run) => {
        this.isNew.set(false);
        void this.router.navigate(['/payroll/runs', run!.id]);
      });
  }

  openRun(): void {
    if (this.openForm.invalid) {
      this.openForm.markAllAsTouched();
      return;
    }

    const { month, year } = this.openForm.getRawValue();
    this.store.dispatch(PayrollActions.openRun({ month, year }));
  }

  freezeAttendance(): void {
    const payrollRunId = this.run()?.id;
    if (!payrollRunId) {
      return;
    }

    this.store.dispatch(
      PayrollActions.freezeAttendance({ payrollRunId, overrideReason: this.overrideReason() || undefined }),
    );
  }

  runDryRun(): void {
    const payrollRunId = this.run()?.id;
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.runDryRun({ payrollRunId }));
    }
  }

  submitForReview(): void {
    const payrollRunId = this.run()?.id;
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.submitForReview({ payrollRunId }));
    }
  }

  approve(): void {
    const payrollRunId = this.run()?.id;
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.approveRun({ payrollRunId }));
    }
  }

  finalize(): void {
    const payrollRunId = this.run()?.id;
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.finalizeRun({ payrollRunId }));
    }
  }

  publish(): void {
    const payrollRunId = this.run()?.id;
    if (payrollRunId) {
      this.store.dispatch(PayrollActions.publishRun({ payrollRunId }));
    }
  }
}
