using Bannerlord.ModuleManager;
using FluentAssertions;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Emitters;

public class BuiltInEmitterTests : ALoadoutDiagnosticEmitterTest<MountAndBlade2Bannerlord, BuiltInEmitter>
{
    public BuiltInEmitterTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_Emitter()
    {
        var loadout = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadout.AddNative(context);
        await loadout.AddButterLib(context);
        await loadout.AddHarmony(context);

        var diagnostics = await GetAllDiagnostics(loadout);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_Emitter2()
    {
        var loadout = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadout.AddNative(context);
        var modA = await loadout.AddButterLib(context);

        Refresh(ref loadout);

        var diagnostic = await GetSingleDiagnostic(loadout);
        diagnostic.Id.Should().Be(new DiagnosticId(BuiltInEmitter.Source, (ushort) ModuleIssueType.MissingDependencies));
        diagnostic.DataReferences.Values.Should().Equal(
            loadout.ToReference(),
            modA.ToReference(loadout)
        );
    }
}
