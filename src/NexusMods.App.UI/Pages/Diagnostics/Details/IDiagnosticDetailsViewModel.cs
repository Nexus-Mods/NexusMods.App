using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

public interface IDiagnosticDetailsViewModel : IPageViewModelInterface
{
    string Details { get; }
    
    DiagnosticSeverity Severity { get; }

    ReactiveCommand<string, Unit> MarkdownOpenLinkCommand { get; }
}
