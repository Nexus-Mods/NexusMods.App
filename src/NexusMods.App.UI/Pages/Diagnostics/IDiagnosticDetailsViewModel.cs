using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diagnostics;

public interface IDiagnosticDetailsViewModel : IPageViewModelInterface
{
    string Details { get; }
    
    DiagnosticSeverity Severity { get; }
    
}
