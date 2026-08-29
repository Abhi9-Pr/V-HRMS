import {
  CandidateStatus,
  InterviewStatus,
  JobRequisitionStatus,
  OfferLetterStatus,
  RequisitionApprovalStatus,
} from 'vespera-shared';

/**
 * These backend enums have no JsonStringEnumConverter, so NSwag generates them as bare numeric
 * enums (`_0`, `_1`, ...) with no display name recoverable from the generated file itself. Keyed
 * by the exact C# enum declaration order (Vespera.Domain.Recruitment.JobRequisitionStatus /
 * RequisitionApprovalStatus / Candidate.CandidateStatus / Interview.InterviewStatus /
 * OfferLetter.OfferLetterStatus) — if the backend enum order ever changes, these must change
 * with it. Same pattern as expenses.labels.ts/assets.labels.ts.
 */
export const JOB_REQUISITION_STATUS_LABELS: Record<JobRequisitionStatus, string> = {
  [JobRequisitionStatus._0]: 'Open',
  [JobRequisitionStatus._1]: 'On hold',
  [JobRequisitionStatus._2]: 'Closed',
  [JobRequisitionStatus._3]: 'Filled',
};

export const REQUISITION_APPROVAL_STATUS_LABELS: Record<RequisitionApprovalStatus, string> = {
  [RequisitionApprovalStatus._0]: 'Draft',
  [RequisitionApprovalStatus._1]: 'Pending approval',
  [RequisitionApprovalStatus._2]: 'Approved',
  [RequisitionApprovalStatus._3]: 'Rejected',
};

export const CANDIDATE_STATUS_LABELS: Record<CandidateStatus, string> = {
  [CandidateStatus._0]: 'New',
  [CandidateStatus._1]: 'In pipeline',
  [CandidateStatus._2]: 'Offered',
  [CandidateStatus._3]: 'Hired',
  [CandidateStatus._4]: 'Rejected',
  [CandidateStatus._5]: 'Withdrawn',
};

export const INTERVIEW_STATUS_LABELS: Record<InterviewStatus, string> = {
  [InterviewStatus._0]: 'Scheduled',
  [InterviewStatus._1]: 'Completed',
  [InterviewStatus._2]: 'Cancelled',
  [InterviewStatus._3]: 'No show',
};

export const OFFER_LETTER_STATUS_LABELS: Record<OfferLetterStatus, string> = {
  [OfferLetterStatus._0]: 'Draft',
  [OfferLetterStatus._1]: 'Sent',
  [OfferLetterStatus._2]: 'Accepted',
  [OfferLetterStatus._3]: 'Declined',
  [OfferLetterStatus._4]: 'Withdrawn',
};
