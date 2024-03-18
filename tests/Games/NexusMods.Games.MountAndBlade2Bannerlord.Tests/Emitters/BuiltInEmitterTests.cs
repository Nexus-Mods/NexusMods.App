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
        var loadoutMarker = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddNative(context);
        await loadoutMarker.AddButterLib(context);
        await loadoutMarker.AddHarmony(context);

        var diagnostics = await GetAllDiagnostics(loadoutMarker.Value);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_Emitter2()
    {
        var loadoutMarker = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddNative(context);
        var modA = await loadoutMarker.AddButterLib(context);

        var diagnostic = await GetSingleDiagnostic(loadoutMarker.Value);
        diagnostic.Id.Should().Be(new DiagnosticId(BuiltInEmitter.Source, (ushort) ModuleIssueType.MissingDependencies));
        diagnostic.DataReferences.Values.Should().Equal(
            loadoutMarker.Value.ToReference(),
            modA.ToReference(loadoutMarker.Value)
        );
    }
}
