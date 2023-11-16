using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using FluentAssertions;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Emitters;

public class BuiltInEmitterTests : ALoadoutDiagnosticEmitterTest<MountAndBlade2Bannerlord, BuiltInEmitter>
{
    public BuiltInEmitterTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_Emitter()
    {
        var loadoutMarker = await CreateLoadout();

        var modAModuleInfo = new ModuleInfoExtendedWithPath
        {
            Id = "Bannerlord.ButterLib",
            Name = "ButterLib",
            Version = ApplicationVersion.TryParse("v1.0.0.0", out var bVersion) ? bVersion : ApplicationVersion.Empty,
            DependentModuleMetadatas = new []
            {
                new DependentModuleMetadata("Bannerlord.Harmony", LoadType.LoadBeforeThis, false, false, ApplicationVersion.TryParse("v3.0.0.0", out var a2Version) ? a2Version : ApplicationVersion.Empty, ApplicationVersionRange.Empty)
            }
        };
        var modAFiles = TestHelper.CreateTestFiles(modAModuleInfo);
        await using var modAPath = await CreateTestArchive(modAFiles);

        var modA = await InstallModStoredFileIntoLoadout(loadoutMarker, modAPath);

        var diagnostic = await GetSingleDiagnostic(loadoutMarker.Value);
        diagnostic.Id.Should().Be(new DiagnosticId(BuiltInEmitter.Source, (ushort) ModuleIssueType.MissingDependencies));
        diagnostic.DataReferences.Should().Equal(
            loadoutMarker.Value.ToReference(),
            modA.ToReference(loadoutMarker.Value)
        );

        var modBManifest = new ModuleInfoExtendedWithPath
        {
            Id = "Bannerlord.Harmony",
            Name = "Harmony",
            Version = ApplicationVersion.TryParse("v2.3.0.0", out var aVersion) ? aVersion : ApplicationVersion.Empty,
        };

        var modBFiles = TestHelper.CreateTestFiles(modBManifest);
        await using var modBPath = await CreateTestArchive(modBFiles);

        await InstallModStoredFileIntoLoadout(loadoutMarker, modBPath);

        var diagnostics = await GetAllDiagnostics(loadoutMarker.Value);
        diagnostics.Should().BeEmpty();
    }
}
