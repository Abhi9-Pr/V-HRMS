using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Security;

namespace Vespera.Infrastructure.UnitTests.Security;

public class VirusScanServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("ClamAv", typeof(ClamAvVirusScanner))]
    [InlineData("Null", typeof(NullVirusScanner))]
    [InlineData("SomeUnknownProvider", typeof(NullVirusScanner))]
    public void AddVesperaVirusScanning_Should_Register_The_Adapter_Matching_The_Configured_Provider(string provider, Type expectedType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"{VirusScanOptions.SectionName}:Provider"] = provider })
            .Build();

        var services = new ServiceCollection();
        services.AddVesperaVirusScanning(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var resolved = serviceProvider.GetRequiredService<IVirusScanner>();

        resolved.Should().BeOfType(expectedType);
    }
}
