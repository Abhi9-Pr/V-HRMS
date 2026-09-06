import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { TimezoneDatePipe } from '../../../shared/pipes/timezone-date.pipe';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { CANDIDATE_STATUS_LABELS, INTERVIEW_STATUS_LABELS, OFFER_LETTER_STATUS_LABELS } from '../recruitment.labels';
import { ApiError, CandidateStatus, Currency, InterviewDto, OfferLetterDto, OfferLetterStatus } from 'vespera-shared';

@Component({
    selector: 'vespera-candidate-detail',
    imports: [
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatButtonModule,
        FormErrorComponent,
        TimezoneDatePipe,
        ErrorStateComponent,
        LoadingStateComponent,
    ],
    templateUrl: './candidate-detail.component.html'
})
export class CandidateDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly recruitmentFacade = inject(RecruitmentFacade);

  readonly OfferLetterStatus = OfferLetterStatus;
  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly candidateStatusLabel = (status: CandidateStatus | undefined): string =>
    status !== undefined ? CANDIDATE_STATUS_LABELS[status] : '';
  readonly interviewStatusLabel = (status: InterviewDto['status']): string =>
    status !== undefined ? INTERVIEW_STATUS_LABELS[status] : '';
  readonly offerStatusLabel = (status: OfferLetterDto['status']): string =>
    status !== undefined ? OFFER_LETTER_STATUS_LABELS[status] : '';

  readonly candidate = this.recruitmentFacade.candidate;
  readonly candidateLoading = this.recruitmentFacade.candidateLoading;
  readonly candidateError = this.recruitmentFacade.candidateError;

  readonly interviews = this.recruitmentFacade.interviews;
  readonly offers = this.recruitmentFacade.offers;

  readonly interviewForm = this.formBuilder.nonNullable.group({
    pipelineStageId: ['', [Validators.required]],
    scheduledAt: [new Date(), [Validators.required]],
    interviewerIds: ['', [Validators.required]],
  });
  readonly scheduling = signal(false);
  readonly scheduleError = signal<string | null>(null);

  readonly offerForm = this.formBuilder.nonNullable.group({
    proposedDesignationId: ['', [Validators.required]],
    proposedCtc: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency._0, [Validators.required]],
    joiningDate: [new Date(), [Validators.required]],
  });
  readonly creatingOffer = signal(false);
  readonly offerError = signal<string | null>(null);

  readonly convertForm = this.formBuilder.nonNullable.group({
    offerLetterId: ['', [Validators.required]],
    employeeCode: ['', [Validators.required]],
    dateOfBirth: [new Date(), [Validators.required]],
    locationId: ['', [Validators.required]],
  });
  readonly converting = signal(false);
  readonly convertError = signal<string | null>(null);
  readonly convertedEmployeeId = signal<string | null>(null);

  readonly actionInFlight = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  private candidateId = '';

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.recruitmentFacade.loadCandidateById(this.candidateId);
    this.recruitmentFacade.loadInterviewsForCandidate(this.candidateId);
    this.recruitmentFacade.loadOffersForCandidate(this.candidateId);
  }

  scheduleInterview(): void {
    if (this.interviewForm.invalid || this.scheduling()) {
      this.interviewForm.markAllAsTouched();
      return;
    }

    this.scheduling.set(true);
    this.scheduleError.set(null);

    const { pipelineStageId, scheduledAt, interviewerIds } = this.interviewForm.getRawValue();

    this.recruitmentFacade
      .scheduleInterview({
        candidateId: this.candidateId,
        pipelineStageId,
        scheduledAt,
        interviewerIds: interviewerIds
          .split(',')
          .map((id) => id.trim())
          .filter((id) => id.length > 0),
      })
      .pipe(
        catchError((apiError: ApiError) => {
          this.scheduleError.set(apiError.message);
          this.scheduling.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.scheduling.set(false);
        this.interviewForm.reset({ pipelineStageId: '', scheduledAt: new Date(), interviewerIds: '' });
        this.reload();
      });
  }

  submitScorecard(interview: InterviewDto, interviewerId: string, rating: number, notes: string): void {
    if (!interview.id) {
      return;
    }

    this.runAction(
      interview.id,
      this.recruitmentFacade.submitScorecard(interview.id, { interviewerId, rating, notes: notes || undefined }),
    );
  }

  completeInterview(interview: InterviewDto, feedback: string, rating: number): void {
    if (!interview.id) {
      return;
    }

    this.runAction(interview.id, this.recruitmentFacade.completeInterview(interview.id, { feedback, rating }));
  }

  cancelInterview(interview: InterviewDto): void {
    if (!interview.id) {
      return;
    }

    this.runAction(interview.id, this.recruitmentFacade.cancelInterview(interview.id));
  }

  rescheduleInterview(interview: InterviewDto, newScheduledAt: Date): void {
    if (!interview.id) {
      return;
    }

    this.runAction(interview.id, this.recruitmentFacade.rescheduleInterview(interview.id, { newScheduledAt }));
  }

  createOffer(): void {
    if (this.offerForm.invalid || this.creatingOffer()) {
      this.offerForm.markAllAsTouched();
      return;
    }

    this.creatingOffer.set(true);
    this.offerError.set(null);

    this.recruitmentFacade
      .createOffer({ candidateId: this.candidateId, ...this.offerForm.getRawValue() })
      .pipe(
        catchError((apiError: ApiError) => {
          this.offerError.set(apiError.message);
          this.creatingOffer.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.creatingOffer.set(false);
        this.offerForm.reset({
          proposedDesignationId: '',
          proposedCtc: 0,
          currency: Currency._0,
          joiningDate: new Date(),
        });
        this.reload();
      });
  }

  sendOffer(offer: OfferLetterDto): void {
    if (!offer.id) {
      return;
    }

    this.runAction(offer.id, this.recruitmentFacade.sendOffer(offer.id));
  }

  acceptOffer(offer: OfferLetterDto): void {
    if (!offer.id) {
      return;
    }

    this.runAction(offer.id, this.recruitmentFacade.acceptOffer(offer.id));
  }

  declineOffer(offer: OfferLetterDto): void {
    if (!offer.id) {
      return;
    }

    this.runAction(offer.id, this.recruitmentFacade.declineOffer(offer.id));
  }

  withdrawOffer(offer: OfferLetterDto): void {
    if (!offer.id) {
      return;
    }

    this.runAction(offer.id, this.recruitmentFacade.withdrawOffer(offer.id));
  }

  downloadOfferPdf(offer: OfferLetterDto): void {
    if (!offer.id) {
      return;
    }

    this.recruitmentFacade.downloadOfferPdf(offer.id).subscribe((file) => {
      if (!file.data) {
        return;
      }

      const url = URL.createObjectURL(file.data);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = file.fileName ?? 'offer-letter.pdf';
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  convertToEmployee(): void {
    if (this.convertForm.invalid || this.converting()) {
      this.convertForm.markAllAsTouched();
      return;
    }

    this.converting.set(true);
    this.convertError.set(null);

    this.recruitmentFacade
      .convertToEmployee({ candidateId: this.candidateId, ...this.convertForm.getRawValue() })
      .pipe(
        catchError((apiError: ApiError) => {
          this.convertError.set(apiError.message);
          this.converting.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.converting.set(false);
        this.convertedEmployeeId.set(result.id ?? null);
      });
  }

  private runAction(key: string, action$: ReturnType<RecruitmentFacade['cancelInterview']>): void {
    this.actionInFlight.set(key);
    this.actionError.set(null);

    action$
      .pipe(
        catchError((apiError: ApiError) => {
          this.actionError.set(apiError.message);
          this.actionInFlight.set(null);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.actionInFlight.set(null);
        this.reload();
      });
  }
}
