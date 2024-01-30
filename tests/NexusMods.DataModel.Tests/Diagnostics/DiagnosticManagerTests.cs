using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.Tests.Harness;

namespace NexusMods.DataModel.Tests.Diagnostics;

public class DiagnosticManagerTests : ADataModelTest<DiagnosticManagerTests>
{
    private readonly DiagnosticManager _diagnosticManager;

    public DiagnosticManagerTests(IServiceProvider provider) : base(provider)
    {
        _diagnosticManager = provider.GetRequiredService<DiagnosticManager>();
    }

    [Fact]
    public async Task Test_RefreshLoadoutDiagnostics()
    {
        _diagnosticManager.ClearDiagnostics();
        _diagnosticManager.ActiveDiagnostics.Should().BeEmpty();

        var called = false;
        using var disposable = _diagnosticManager.DiagnosticChanges.Subscribe(changeSet =>
        {
            called.Should().BeFalse();
            called = true;
            changeSet.Adds.Should().Be(1);
        });

        await _diagnosticManager.RefreshLoadoutDiagnostics(BaseList.Value);

        var diagnostic = _diagnosticManager.ActiveDiagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Number.Should().Be(1);
        diagnostic.Message.Should().Be(DummyLoadoutDiagnosticEmitter.CreateMessage(BaseList.Value));
    }

    [Fact]
    public async Task Test_RefreshModDiagnostics()
    {
        var mod = await AddDummyMod(BaseList);
        BaseList.Value.Mods.Count.Should().Be(2);

        _diagnosticManager.ClearDiagnostics();
        _diagnosticManager.ActiveDiagnostics.Should().BeEmpty();

        _diagnosticManager.RefreshModDiagnostics(BaseList.Value);

        var diagnostic = _diagnosticManager.ActiveDiagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Number.Should().Be(1);
        diagnostic.Message.Should().Be(DummyModDiagnosticEmitter.CreateMessage(BaseList.Value, mod));

        BaseList.Remove(mod);
        BaseList.Value.Mods.Count.Should().Be(1);

        _diagnosticManager.RefreshModDiagnostics(BaseList.Value);
        _diagnosticManager.ActiveDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Test_RefreshModFileDiagnostics()
    {
        var act = () => _diagnosticManager.RefreshModFileDiagnostics();
        act.Should().ThrowExactly<NotImplementedException>();
    }

    [Fact]
    public void Test_FilterDiagnostics_MinimumSeverity()
    {
        var options = new DiagnosticOptions
        {
            MinimumSeverity = DiagnosticSeverity.Critical
        };

        var diagnostics = new Diagnostic[]
        {
            new()
            {
                Id = new DiagnosticId(),
                Message = DiagnosticMessage.From(""),
                Severity = DiagnosticSeverity.Warning,
                DataReferences = ImmutableArray<IDataReference>.Empty
            }
        };

        var result = DiagnosticManager.FilterDiagnostics(diagnostics, options);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Test_FilterDiagnostics_IgnoredDiagnostics()
    {
        var options = new DiagnosticOptions
        {
            IgnoredDiagnostics = new HashSet<DiagnosticId>
            {
                new("foo", 1)
            }.ToImmutableHashSet()
        };

        var diagnostics = new Diagnostic[]
        {
            new()
            {
                Id = new DiagnosticId("foo", 1),
                Message = DiagnosticMessage.From(""),
                Severity = DiagnosticSeverity.Warning,
                DataReferences = ImmutableArray<IDataReference>.Empty
            }
        };

        var result = DiagnosticManager.FilterDiagnostics(diagnostics, options);
        result.Should().BeEmpty();
    }

    private async Task<Mod> AddDummyMod(LoadoutMarker loadout)
    {
        var ids = await AddMods(loadout, DataZipLzma, "First Mod");
        var id = ids.Should().ContainSingle().Which;
        return loadout.Value.Mods[id];
    }
}
