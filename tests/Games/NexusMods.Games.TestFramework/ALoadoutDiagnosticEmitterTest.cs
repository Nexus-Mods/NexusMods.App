using FluentAssertions;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.TestFramework;

public class ALoadoutDiagnosticEmitterTest<TGame, TEmitter> : AGameTest<TGame>
    where TGame : AGame
    where TEmitter : ILoadoutDiagnosticEmitter
{
    protected readonly TEmitter Emitter;

    protected ALoadoutDiagnosticEmitterTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Emitter = serviceProvider.FindImplementationInContainer<TEmitter, ILoadoutDiagnosticEmitter>();
    }

    protected async ValueTask<Diagnostic[]> GetAllDiagnostics(Loadout loadout)
    {
        return await Emitter.Diagnose(loadout).ToArrayAsync();
    }

    protected async ValueTask<Diagnostic> GetSingleDiagnostic(Loadout loadout)
    {
        var diagnostics = await GetAllDiagnostics(loadout);
        diagnostics.Should().ContainSingle();
        return diagnostics.First();
    }
}
