using Vespera.Domain.Eis;

namespace Vespera.Domain.Recruitment;

public sealed record InterviewScorecard(EmployeeId InterviewerId, int Rating, string? Notes, DateTimeOffset SubmittedAt);
