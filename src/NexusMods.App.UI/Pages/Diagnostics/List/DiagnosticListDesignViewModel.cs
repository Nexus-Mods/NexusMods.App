using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
internal class DiagnosticListDesignViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    public LoadoutId LoadoutId { get; set; } = new LoadoutId();

    public IDiagnosticEntryViewModel[] DiagnosticEntries { get; } = new IDiagnosticEntryViewModel[]
    {
        new DiagnosticEntryDesignViewModel(),
        new DiagnosticEntryDesignViewModel(),
        new DiagnosticEntryDesignViewModel(),
    };

    public int NumCritical { get; } = 1;
    public int NumWarnings { get; } = 1;
    public int NumSuggestions { get; } = 1;

    public DiagnosticFilter Filter { get; } = DiagnosticFilter.Critical | DiagnosticFilter.Warnings | DiagnosticFilter.Suggestions;

    public ReactiveCommand<DiagnosticSeverity, Unit> ToggleSeverityCommand { get; } = ReactiveCommand.Create<DiagnosticSeverity>(_ => { });
    public ReactiveCommand<Unit, Unit> ShowAllCommand { get; } = ReactiveCommand.Create(() => { });
}
