using FluentAssertions;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.StardewValley.Tests.Emitters;

public class MissingDependenciesEmitterTests : ALoadoutDiagnosticEmitterTest<StardewValley, MissingDependenciesEmitter>
{
    public MissingDependenciesEmitterTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_Emitter()
    {
        var loadoutMarker = await CreateLoadout();

        var modAManifest = new SMAPIManifest
        {
            Name = "ModA",
            UniqueID = "ModA",
            Version = new Version(1, 0, 0),
            Dependencies = new[]
            {
                new SMAPIManifestDependency
                {
                    UniqueID = "ModB"
                }
            }
        };

        var modAFiles = TestHelper.CreateTestFiles(modAManifest);
        await using var modAPath = await CreateTestArchive(modAFiles);

        var modA = await InstallModFromArchiveIntoLoadout(loadoutMarker, modAPath);

        var diagnostic = GetSingleDiagnostic(loadoutMarker.Value);
        diagnostic.Id.Should().Be(new DiagnosticId(Diagnostics.Source, 1));
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Message.Should().Be(DiagnosticMessage.From($"Mod 'ModA' is missing required dependency 'ModB'"));
        diagnostic.DataReferences.Should().Equal(
            loadoutMarker.Value.ToReference(),
            modA.ToReference(loadoutMarker.Value)
        );

        var modBManifest = new SMAPIManifest
        {
            Name = "ModB",
            UniqueID = "ModB",
            Version = new Version(1, 0, 0)
        };

        var modBFiles = TestHelper.CreateTestFiles(modBManifest);
        await using var modBPath = await CreateTestArchive(modBFiles);

        await InstallModFromArchiveIntoLoadout(loadoutMarker, modBPath);

        var diagnostics = GetAllDiagnostics(loadoutMarker.Value);
        diagnostics.Should().BeEmpty();
    }
}
