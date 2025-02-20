using FluentAssertions;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using StardewModdingAPI.Toolkit;

namespace NexusMods.Games.StardewValley.Tests;

public class SMAPIGameVersionDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<StardewValley, SMAPIGameVersionDiagnosticEmitter>
{
    public SMAPIGameVersionDiagnosticEmitterTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

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
