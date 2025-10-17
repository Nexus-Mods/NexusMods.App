using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public interface IDiagnosticEntryViewModel : IViewModelInterface
{
    Diagnostic Diagnostic { get; }

    string Title { get; }
    
    string Summary { get; }
    
    DiagnosticSeverity Severity { get; }
    
    ReactiveCommand<NavigationInformation, ValueTuple<Diagnostic, NavigationInformation>> SeeDetailsCommand { get; }
}

