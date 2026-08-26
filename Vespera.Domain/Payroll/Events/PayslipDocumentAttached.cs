using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayslipDocumentAttached(
    PayslipId PayslipId, TenantId TenantId, EmployeeId EmployeeId, PayrollRunId PayrollRunId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
