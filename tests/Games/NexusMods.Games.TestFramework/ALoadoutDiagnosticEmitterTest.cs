using FluentAssertions;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;

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
