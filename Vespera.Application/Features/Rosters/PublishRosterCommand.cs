using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Rosters;

/// <summary>Publishes every <c>Draft</c> roster row for the given employees overlapping the given
/// date range. There is no roster "batch" concept to key off — a generation run isn't tagged with
/// an id — so publishing is scoped by employees + range instead, which is exactly what
/// <c>GenerateRosterCommand</c> just produced.</summary>
public sealed record PublishRosterCommand(
    IReadOnlyList<Guid> EmployeeIds,
    DateOnly RangeStart,
    DateOnly RangeEnd) : IRequest<Result<int>>;
