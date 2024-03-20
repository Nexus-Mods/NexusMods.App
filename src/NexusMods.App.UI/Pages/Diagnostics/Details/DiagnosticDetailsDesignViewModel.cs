using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsDesignViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    public string Details { get; } = "This is an example diagnostic details, lots of stuff here.";
    public DiagnosticSeverity Severity { get; } = DiagnosticSeverity.Critical;
    
    public DiagnosticDetailsDesignViewModel() : base(new DesignWindowManager()) { }
}
