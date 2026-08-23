using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave.Events;

/// <summary>Raised when a step becomes the chain's current, actionable step — on
/// <see cref="ApprovalChain.Create"/> (step 0) and again each time <see cref="ApprovalChain.Approve"/>
/// advances to the next tier. Subject-type-agnostic (the chain itself is), so a notification handler
/// filters on <see cref="SubjectType"/> to decide whether it applies.</summary>
public sealed record ApprovalStepAssigned(
    ApprovalChainId ChainId, TenantId TenantId, ApprovalSubjectType SubjectType, Guid SubjectId, int StepIndex,
    EmployeeId ApproverId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
