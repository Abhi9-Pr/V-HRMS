using Vespera.Domain.Common;

namespace Vespera.Domain.Leave.Events;

public sealed record ApprovalChainApproved(
    ApprovalChainId ChainId, TenantId TenantId, ApprovalSubjectType SubjectType, Guid SubjectId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
