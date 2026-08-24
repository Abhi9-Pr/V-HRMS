import { Currency } from '../../core/models/currency';
import { PagedRequest, PagedResult } from '../expenses/expenses.models';

// Mirrors Vespera.Domain.Recruitment.JobRequisition.JobRequisitionStatus exactly (ordinal values
// — the API has no JsonStringEnumConverter registered, see currency.ts).
export enum JobRequisitionStatus {
  Open = 0,
  OnHold = 1,
  Closed = 2,
  Filled = 3,
}

export const JOB_REQUISITION_STATUS_LABELS: Record<JobRequisitionStatus, string> = {
  [JobRequisitionStatus.Open]: 'Open',
  [JobRequisitionStatus.OnHold]: 'On Hold',
  [JobRequisitionStatus.Closed]: 'Closed',
  [JobRequisitionStatus.Filled]: 'Filled',
};

// Mirrors Vespera.Domain.Recruitment.RequisitionApprovalStatus — orthogonal to JobRequisitionStatus.
export enum RequisitionApprovalStatus {
  Draft = 0,
  PendingApproval = 1,
  Approved = 2,
  Rejected = 3,
}

export const REQUISITION_APPROVAL_STATUS_LABELS: Record<RequisitionApprovalStatus, string> = {
  [RequisitionApprovalStatus.Draft]: 'Draft',
  [RequisitionApprovalStatus.PendingApproval]: 'Pending Approval',
  [RequisitionApprovalStatus.Approved]: 'Approved',
  [RequisitionApprovalStatus.Rejected]: 'Rejected',
};

// Mirrors Vespera.Domain.Recruitment.Candidate.CandidateStatus.
export enum CandidateStatus {
  New = 0,
  InPipeline = 1,
  Offered = 2,
  Hired = 3,
  Rejected = 4,
  Withdrawn = 5,
}

export const CANDIDATE_STATUS_LABELS: Record<CandidateStatus, string> = {
  [CandidateStatus.New]: 'New',
  [CandidateStatus.InPipeline]: 'In Pipeline',
  [CandidateStatus.Offered]: 'Offered',
  [CandidateStatus.Hired]: 'Hired',
  [CandidateStatus.Rejected]: 'Rejected',
  [CandidateStatus.Withdrawn]: 'Withdrawn',
};

// Mirrors Vespera.Domain.Recruitment.Interview.InterviewStatus.
export enum InterviewStatus {
  Scheduled = 0,
  Completed = 1,
  Cancelled = 2,
  NoShow = 3,
}

export const INTERVIEW_STATUS_LABELS: Record<InterviewStatus, string> = {
  [InterviewStatus.Scheduled]: 'Scheduled',
  [InterviewStatus.Completed]: 'Completed',
  [InterviewStatus.Cancelled]: 'Cancelled',
  [InterviewStatus.NoShow]: 'No Show',
};

// Mirrors Vespera.Domain.Recruitment.OfferLetter.OfferLetterStatus.
export enum OfferLetterStatus {
  Draft = 0,
  Sent = 1,
  Accepted = 2,
  Declined = 3,
  Withdrawn = 4,
}

export const OFFER_LETTER_STATUS_LABELS: Record<OfferLetterStatus, string> = {
  [OfferLetterStatus.Draft]: 'Draft',
  [OfferLetterStatus.Sent]: 'Sent',
  [OfferLetterStatus.Accepted]: 'Accepted',
  [OfferLetterStatus.Declined]: 'Declined',
  [OfferLetterStatus.Withdrawn]: 'Withdrawn',
};

export interface PipelineStageDto {
  id: string;
  name: string;
  sequenceNumber: number;
}

export interface JobRequisitionDto {
  id: string;
  title: string;
  departmentId: string;
  openingsCount: number;
  status: JobRequisitionStatus;
  approvalStatus: RequisitionApprovalStatus;
  isPublished: boolean;
  rejectionReason: string | null;
  stages: PipelineStageDto[];
}

export interface CreateRequisitionRequest {
  title: string;
  departmentId: string;
  openingsCount: number;
}

export interface AddStageRequest {
  stageName: string;
}

export interface DecideRequisitionApprovalRequest {
  approved: boolean;
  comment: string | null;
}

export interface CandidateCardDto {
  id: string;
  fullName: string;
  status: CandidateStatus;
  currentPipelineStageId: string | null;
}

export interface CandidatePipelineDto {
  stages: PipelineStageDto[];
  candidates: CandidateCardDto[];
}

export interface CandidateDto {
  id: string;
  jobRequisitionId: string;
  fullName: string;
  email: string;
  phone: string;
  status: CandidateStatus;
  currentPipelineStageId: string | null;
}

export interface CreateCandidateRequest {
  jobRequisitionId: string;
  fullName: string;
  email: string;
  phone: string;
}

export interface MoveToStageRequest {
  targetStageId: string;
}

export interface InterviewScorecardDto {
  interviewerId: string;
  rating: number;
  notes: string | null;
  submittedAt: string;
}

export interface InterviewDto {
  id: string;
  candidateId: string;
  pipelineStageId: string;
  scheduledAt: string;
  interviewerIds: string[];
  status: InterviewStatus;
  feedback: string | null;
  rating: number | null;
  scorecards: InterviewScorecardDto[];
}

export interface ScheduleInterviewRequest {
  candidateId: string;
  pipelineStageId: string;
  scheduledAt: string;
  interviewerIds: string[];
}

export interface SubmitScorecardRequest {
  interviewerId: string;
  rating: number;
  notes: string | null;
}

export interface CompleteInterviewRequest {
  feedback: string;
  rating: number;
}

export interface RescheduleInterviewRequest {
  newScheduledAt: string;
}

// NOTE: unlike every other enum in this DTO, OfferLetterDto.currency is a STRING (the API maps
// Money.Currency via .ToString(), not the numeric ordinal) — see OfferLetterMappingConfig.cs.
export interface OfferLetterDto {
  id: string;
  candidateId: string;
  proposedDesignationId: string;
  proposedCtc: number;
  currency: string;
  joiningDate: string;
  status: OfferLetterStatus;
}

export interface CreateOfferRequest {
  candidateId: string;
  proposedDesignationId: string;
  proposedCtc: number;
  currency: Currency;
  joiningDate: string;
}

export interface ConvertToEmployeeRequest {
  candidateId: string;
  offerLetterId: string;
  employeeCode: string;
  dateOfBirth: string;
  locationId: string;
}

export interface PublicJobDto {
  id: string;
  title: string;
  departmentName: string;
  openingsCount: number;
}

export type { PagedRequest, PagedResult };
