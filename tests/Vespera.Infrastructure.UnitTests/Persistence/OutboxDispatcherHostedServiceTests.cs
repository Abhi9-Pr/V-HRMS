using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Eis.Events;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class OutboxDispatcherHostedServiceTests
{
    [Fact]
    public async Task ProcessOnceAsync_Should_Mark_A_Pending_Row_Processed_And_Publish_The_Wrapped_Domain_Event()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(false);
        var publisher = Substitute.For<IPublisher>();
        var notificationDispatcher = Substitute.For<INotificationDispatcher>();

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector());

        var domainEvent = new EmployeeOnboarded(
            Vespera.Domain.Eis.EmployeeId.New(), Vespera.Domain.Common.TenantId.New(), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);
        dbContext.Add(new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOn = DateTimeOffset.UtcNow,
            Status = OutboxMessageStatus.Pending,
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(publisher);
        services.AddSingleton(notificationDispatcher);
        services.AddSingleton<IDateTimeProvider>(new FixedDateTimeProvider(DateTimeOffset.UtcNow));
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new BackgroundJobsOptions());
        var dispatcher = new OutboxDispatcherHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<OutboxDispatcherHostedService>.Instance);

        await dispatcher.ProcessOnceAsync(CancellationToken.None);

        var row = await dbContext.Set<OutboxMessageEntity>().SingleAsync(CancellationToken.None);
        row.Status.Should().Be(OutboxMessageStatus.Processed);
        row.ProcessedAt.Should().NotBeNull();
        // OutboxDispatcherHostedService calls the object-typed IPublisher.Publish overload
        // deliberately (the notification's concrete type is only known at runtime), so the
        // substitute call is recorded under that overload too.
        await publisher.Received(1).Publish(Arg.Is<object>(n => n is DomainEventNotification<EmployeeOnboarded>), Arg.Any<CancellationToken>());
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }
}
