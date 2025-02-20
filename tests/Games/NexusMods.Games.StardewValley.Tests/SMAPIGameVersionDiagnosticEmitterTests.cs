using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using StardewModdingAPI.Toolkit;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

public class SMAPIGameVersionDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<SMAPIGameVersionDiagnosticEmitterTests, StardewValley, SMAPIGameVersionDiagnosticEmitter>
{
    public SMAPIGameVersionDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.4"));
    }

    [Theory]
    [InlineData("1.6.12", "4.1.6")]
    [InlineData("1.5.6", "3.18.6")]
    public async Task Test_TryGetLastSupportedSMAPIVersion(string rawGameVersion, string rawExpectedSMAPIVersion)
    {
        var gameToSMAPIMappings = await Emitter.FetchGameToSMAPIMappings(CancellationToken.None);

        SMAPIGameVersionDiagnosticEmitter.TryGetLastSupportedSMAPIVersion(
            gameToSMAPIMappings!,
            new SemanticVersion(rawGameVersion),
            out var supportedSMAPIVersion
        ).Should().BeTrue();

        supportedSMAPIVersion.Should().NotBeNull();
        supportedSMAPIVersion!.ToString().Should().Be(rawExpectedSMAPIVersion);
    }
}
