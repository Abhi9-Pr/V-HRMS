using FluentAssertions;

namespace Vespera.Application.UnitTests;

public class SanityTests
{
    [Fact]
    public void Application_Assembly_Should_Load()
    {
        typeof(Vespera.Application.AssemblyReference).Assembly.Should().NotBeNull();
    }
}
