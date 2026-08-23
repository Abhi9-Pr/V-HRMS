using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Application.Behaviors;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Behaviors;

public class TenantScopeBehaviorTests
{
    private sealed record TestCommand : IRequest<Result<int>>;

    private sealed record TenantlessTestCommand : IRequest<Result<int>>, ITenantlessRequest;

    [Fact]
    public async Task Handle_Should_Call_Next_When_Tenant_Present()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);

        var behavior = new TenantScopeBehavior<TestCommand, Result<int>>(tenantContext);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Short_Circuit_When_No_Tenant()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(false);

        var behavior = new TenantScopeBehavior<TestCommand, Result<int>>(tenantContext);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Error.Type.Should().Be(ErrorType.Unauthorized);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Call_Next_For_A_Tenantless_Request_Even_Without_A_Tenant()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(false);

        var behavior = new TenantScopeBehavior<TenantlessTestCommand, Result<int>>(tenantContext);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new TenantlessTestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }
}
