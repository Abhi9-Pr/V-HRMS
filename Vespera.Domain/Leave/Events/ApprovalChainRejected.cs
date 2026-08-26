using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave.Events;

public sealed record ApprovalChainRejected(
    ApprovalChainId ChainId, TenantId TenantId, ApprovalSubjectType SubjectType, Guid SubjectId, EmployeeId DecidedBy,
    string Comment, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
