using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public interface IDiagnosticEntryViewModel : IViewModelInterface
{
    string Summary { get; }
    
    DiagnosticSeverity Severity { get; }
    
    ReactiveCommand<Unit, Diagnostic> SeeDetailsCommand { get; }
}

