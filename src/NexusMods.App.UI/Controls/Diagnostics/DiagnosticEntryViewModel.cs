using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryViewModel : AViewModel<IDiagnosticEntryViewModel>, IDiagnosticEntryViewModel
{
    
    public DiagnosticEntryViewModel(Diagnostic diagnostic, IDiagnosticWriter writer)
    {
        Diagnostic = diagnostic;
        Summary = diagnostic.FormatSummary(writer);
        Severity = diagnostic.Severity;
        SeeDetailsCommand = ReactiveCommand.Create(() => diagnostic);
    }

    public Diagnostic Diagnostic { get; }
    public string Summary { get; }
    public DiagnosticSeverity Severity { get; }
    public ReactiveCommand<Unit, Diagnostic> SeeDetailsCommand { get; }
}
