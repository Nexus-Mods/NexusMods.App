using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.Pages.Diagnostics;

public interface IDiagnosticDetailsViewModel : IViewModelInterface
{
    string Details { get; }
    
    DiagnosticSeverity Severity { get; }
    
}
