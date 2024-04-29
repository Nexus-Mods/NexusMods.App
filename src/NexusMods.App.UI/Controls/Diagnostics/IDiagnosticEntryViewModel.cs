using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.Navigation;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public interface IDiagnosticEntryViewModel : IViewModelInterface
{
    Diagnostic Diagnostic { get; }

    string Summary { get; }
    
    DiagnosticSeverity Severity { get; }
    
    ReactiveCommand<NavigationInformation, ValueTuple<Diagnostic, NavigationInformation>> SeeDetailsCommand { get; }
}

