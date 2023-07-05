using FluentAssertions;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Diagnostics.Emitters;
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

    protected Diagnostic[] GetAllDiagnostics(Loadout loadout)
    {
        return Emitter.Diagnose(loadout).ToArray();
    }

    protected Diagnostic GetSingleDiagnostic(Loadout loadout)
    {
        var diagnostics = GetAllDiagnostics(loadout);
        diagnostics.Should().ContainSingle();
        return diagnostics.First();
    }
}
