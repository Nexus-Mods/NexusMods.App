using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsDesignViewModel : AViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    public string Details { get; } = "This is an example diagnostic details, lots of stuff here.";
    public DiagnosticSeverity Severity { get; } = DiagnosticSeverity.Critical;
}
