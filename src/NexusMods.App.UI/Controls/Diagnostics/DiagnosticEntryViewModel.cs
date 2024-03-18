using System.Reactive;
using NexusMods.Abstractions.Diagnostics;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryViewModel : AViewModel<IDiagnosticEntryViewModel>, IDiagnosticEntryViewModel
{
    
    public DiagnosticEntryViewModel(Diagnostic diagnostic)
    {
        Summary = diagnostic.Summary.ToString();
        Severity = diagnostic.Severity;
        SeeDetailsCommand = ReactiveCommand.Create(() => { });
    }

    public string Summary { get; }
    public DiagnosticSeverity Severity { get; }
    public ReactiveCommand<Unit, Unit> SeeDetailsCommand { get; }
}
