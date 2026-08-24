import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { RecruitmentService } from '../recruitment.service';
import {
  CANDIDATE_STATUS_LABELS,
  CandidateDto,
  INTERVIEW_STATUS_LABELS,
  InterviewDto,
  InterviewStatus,
  JobRequisitionDto,
  OFFER_LETTER_STATUS_LABELS,
  OfferLetterDto,
  OfferLetterStatus,
} from '../recruitment.models';

@Component({
  selector: 'app-candidate-detail',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './candidate-detail.html',
  styleUrl: './candidate-detail.scss',
})
export class CandidateDetail implements OnInit {
  protected readonly candidate = signal<CandidateDto | null>(null);
  protected readonly requisition = signal<JobRequisitionDto | null>(null);
  protected readonly interviews = signal<InterviewDto[]>([]);
  protected readonly offers = signal<OfferLetterDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly newEmployeeId = signal<string | null>(null);

  protected readonly currencyOptions = CURRENCY_OPTIONS;

  private readonly fb = inject(FormBuilder);

  protected readonly interviewForm = this.fb.nonNullable.group({
    pipelineStageId: ['', Validators.required],
    scheduledAt: [new Date(), Validators.required],
    interviewerIds: ['', Validators.required],
  });

  protected readonly offerForm = this.fb.nonNullable.group({
    proposedDesignationId: ['', Validators.required],
    proposedCtc: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency.Inr, Validators.required],
    joiningDate: [new Date(), Validators.required],
  });

  protected readonly conversionForm = this.fb.nonNullable.group({
    employeeCode: ['', Validators.required],
    dateOfBirth: [null as Date | null, Validators.required],
    locationId: ['', Validators.required],
  });

  private candidateId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly recruitment: RecruitmentService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.recruitment.getCandidateById(this.candidateId).subscribe((candidate) => {
      this.candidate.set(candidate);
      this.recruitment.getRequisitionById(candidate.jobRequisitionId).subscribe((requisition) => this.requisition.set(requisition));
      this.loadInterviews();
      this.loadOffers();
      this.loading.set(false);
    });
  }

  private loadInterviews(): void {
    this.recruitment.getInterviewsForCandidate(this.candidateId).subscribe((interviews) => this.interviews.set(interviews));
  }

  private loadOffers(): void {
    this.recruitment.getOffersForCandidate(this.candidateId).subscribe((offers) => this.offers.set(offers));
  }

  scheduleInterview(): void {
    if (this.interviewForm.invalid) {
      this.interviewForm.markAllAsTouched();
      return;
    }

    const value = this.interviewForm.getRawValue();
    this.recruitment
      .scheduleInterview({
        candidateId: this.candidateId,
        pipelineStageId: value.pipelineStageId,
        scheduledAt: value.scheduledAt.toISOString(),
        interviewerIds: value.interviewerIds.split(',').map((id) => id.trim()).filter((id) => id.length > 0),
      })
      .subscribe(() => {
        this.interviewForm.reset({ pipelineStageId: '', scheduledAt: new Date(), interviewerIds: '' });
        this.loadInterviews();
      });
  }

  completeInterview(interviewId: string, feedback: string, rating: number): void {
    this.recruitment.completeInterview(interviewId, { feedback, rating }).subscribe(() => this.loadInterviews());
  }

  cancelInterview(interviewId: string): void {
    this.recruitment.cancelInterview(interviewId).subscribe(() => this.loadInterviews());
  }

  submitScorecard(interviewId: string, interviewerId: string, rating: number, notes: string | null): void {
    this.recruitment.submitScorecard(interviewId, { interviewerId, rating, notes }).subscribe(() => this.loadInterviews());
  }

  createOffer(): void {
    if (this.offerForm.invalid) {
      this.offerForm.markAllAsTouched();
      return;
    }

    const value = this.offerForm.getRawValue();
    this.recruitment
      .createOffer({
        candidateId: this.candidateId,
        proposedDesignationId: value.proposedDesignationId,
        proposedCtc: value.proposedCtc,
        currency: value.currency,
        joiningDate: toDateOnly(value.joiningDate),
      })
      .subscribe(() => this.loadOffers());
  }

  sendOffer(offerLetterId: string): void {
    this.recruitment.sendOffer(offerLetterId).subscribe(() => this.loadOffers());
  }

  acceptOffer(offerLetterId: string): void {
    this.recruitment.acceptOffer(offerLetterId).subscribe(() => {
      this.loadOffers();
      this.load();
    });
  }

  declineOffer(offerLetterId: string): void {
    this.recruitment.declineOffer(offerLetterId).subscribe(() => this.loadOffers());
  }

  withdrawOffer(offerLetterId: string): void {
    this.recruitment.withdrawOffer(offerLetterId).subscribe(() => this.loadOffers());
  }

  downloadPdf(offerLetterId: string): void {
    this.recruitment.downloadOfferPdf(offerLetterId).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `offer-${offerLetterId}.pdf`;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  convertToEmployee(offerLetterId: string): void {
    if (this.conversionForm.invalid) {
      this.conversionForm.markAllAsTouched();
      return;
    }

    const value = this.conversionForm.getRawValue();
    this.errorMessage.set(null);
    this.recruitment
      .convertToEmployee({
        candidateId: this.candidateId,
        offerLetterId,
        employeeCode: value.employeeCode,
        dateOfBirth: toDateOnly(value.dateOfBirth!),
        locationId: value.locationId,
      })
      .subscribe({
        next: (result) => {
          this.newEmployeeId.set(result.id);
          this.snackBar.open(`Converted to employee ${result.id}`, 'Dismiss', { duration: 6000 });
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(err.error?.detail ?? 'Conversion failed.');
        },
      });
  }

  protected candidateStatusLabel(status: CandidateDto['status']): string {
    return CANDIDATE_STATUS_LABELS[status];
  }

  protected interviewStatusLabel(status: InterviewStatus): string {
    return INTERVIEW_STATUS_LABELS[status];
  }

  protected offerStatusLabel(status: OfferLetterStatus): string {
    return OFFER_LETTER_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }

  protected canAcceptOrDecline(offer: OfferLetterDto): boolean {
    return offer.status === OfferLetterStatus.Sent;
  }

  protected canSend(offer: OfferLetterDto): boolean {
    return offer.status === OfferLetterStatus.Draft;
  }

  protected canWithdraw(offer: OfferLetterDto): boolean {
    return offer.status === OfferLetterStatus.Draft || offer.status === OfferLetterStatus.Sent;
  }

  protected canConvert(offer: OfferLetterDto): boolean {
    return offer.status === OfferLetterStatus.Accepted;
  }

  protected isScheduled(interview: InterviewDto): boolean {
    return interview.status === InterviewStatus.Scheduled;
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
