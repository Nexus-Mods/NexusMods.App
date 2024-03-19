using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    private Diagnostic Diagnostic { get; set; }
    public string Details { get; }
    public DiagnosticSeverity Severity { get; }

    public DiagnosticDetailsViewModel(IWindowManager windowManager, 
        IDiagnosticWriter diagnosticWriter, 
        Diagnostic diagnostic) : base(windowManager)
    {
        Diagnostic = diagnostic;
        Severity = diagnostic.Severity;

        diagnostic.FormatDetails(diagnosticWriter);
        Details = diagnosticWriter.ToOutput();
    }
}
