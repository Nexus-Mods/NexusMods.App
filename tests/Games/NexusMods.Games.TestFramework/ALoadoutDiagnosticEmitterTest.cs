using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.DiagnosticSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using Xunit.Abstractions;

namespace NexusMods.Games.TestFramework;

public class ALoadoutDiagnosticEmitterTest<TTest, TGame, TEmitter> : AIsolatedGameTest<TTest, TGame>
    where TGame : AGame
    where TEmitter : ILoadoutDiagnosticEmitter
{
    protected readonly TEmitter Emitter;

    protected ALoadoutDiagnosticEmitterTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Emitter = ServiceProvider.FindImplementationInContainer<TEmitter, ILoadoutDiagnosticEmitter>();
    }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services).AddDiagnosticWriter();
    }

    protected async ValueTask<Diagnostic[]> GetAllDiagnostics(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        return await Emitter.Diagnose(loadout, CancellationToken.None).ToArrayAsync();
    }

    protected async ValueTask<Diagnostic> GetSingleDiagnostic(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var diagnostics = await GetAllDiagnostics(loadout);
        diagnostics.Should().ContainSingle();
        return diagnostics.First();
    }

    protected async ValueTask ShouldHaveNoDiagnostics(LoadoutId loadoutId, string because = "")
    {
        var loadout = Loadout.Load(Connection.Db, loadoutId);
        var diagnostics = await Emitter.Diagnose(loadout, CancellationToken.None).ToArrayAsync();
        diagnostics.Should().BeEmpty(because: because);
    }

    protected async ValueTask EnableMod(EntityId entityId)
    {
        using var tx = Connection.BeginTransaction();
        tx.Retract(entityId, LoadoutItem.Disabled, Null.Instance);
        await tx.Commit();
    }

    protected async ValueTask DisabledMod(EntityId entityId)
    {
        using var tx = Connection.BeginTransaction();
        tx.Add(entityId, LoadoutItem.Disabled, Null.Instance);
        await tx.Commit();
    }

    protected async ValueTask VerifyDiagnostic(Diagnostic diagnostic, [CallerFilePath] string sourceFile = "")
    {
        var diagnosticWriter = ServiceProvider.GetRequiredService<IDiagnosticWriter>();

        var summary = diagnostic.FormatSummary(diagnosticWriter);
        var details = diagnostic.FormatDetails(diagnosticWriter);

        var text = $"[Id] {diagnostic.Id}\n[Title] {diagnostic.Title}\n[Summary] {summary}\n[Details]\n{details}";

        // ReSharper disable once ExplicitCallerInfoArgument
        await Verify(text, sourceFile: sourceFile);
    }
}
