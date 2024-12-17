using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
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
        new DiagnosticEntryDesignViewModel(new Diagnostic
        {
            Id = new DiagnosticId(),
            Title = "Example Diagnostic Title 1",
            Severity = DiagnosticSeverity.Warning,
            Summary = DiagnosticMessage.From("Example diagnostic summary 1"),
            Details = DiagnosticMessage.DefaultValue,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>(),
        }),
        new DiagnosticEntryDesignViewModel(new Diagnostic
        {
            Id = new DiagnosticId(),
            Title = "Example Diagnostic Title 2",
            Severity = DiagnosticSeverity.Critical,
            Summary = DiagnosticMessage.From("Example diagnostic summary 2"),
            Details = DiagnosticMessage.DefaultValue,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>(),
        }),
        new DiagnosticEntryDesignViewModel(new Diagnostic
        {
            Id = new DiagnosticId(),
            Title = "Example Diagnostic Title 3",
            Severity = DiagnosticSeverity.Suggestion,
            Summary = DiagnosticMessage.From("Example diagnostic summary 3"),
            Details = DiagnosticMessage.DefaultValue,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>(),
        }),
    };

    public int NumCritical { get; } = 1;
    public int NumWarnings { get; } = 1;
    public int NumSuggestions { get; } = 1;

    public DiagnosticFilter Filter { get; } = DiagnosticFilter.Critical | DiagnosticFilter.Warnings | DiagnosticFilter.Suggestions;

    public ReactiveCommand<DiagnosticSeverity, Unit> ToggleSeverityCommand { get; } = ReactiveCommand.Create<DiagnosticSeverity>(_ => { });
    public ReactiveCommand<Unit, Unit> ShowAllCommand { get; } = ReactiveCommand.Create(() => { });
}
