using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Common;

/// <summary>
/// Wraps a raised <see cref="DomainEvent"/> as a MediatR notification so it can be published once
/// the outbox dispatcher replays it. Domain events themselves stay MediatR-free (Domain has zero
/// external dependencies) — this wrapper is the only place the two meet, and it lives in
/// Application, not Domain.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : DomainEvent;
