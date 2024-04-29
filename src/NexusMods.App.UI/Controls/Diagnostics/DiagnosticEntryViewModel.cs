using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.Navigation;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryViewModel : AViewModel<IDiagnosticEntryViewModel>, IDiagnosticEntryViewModel
{
    
    public DiagnosticEntryViewModel(Diagnostic diagnostic, IDiagnosticWriter writer)
    {
        Diagnostic = diagnostic;
        Summary = diagnostic.FormatSummary(writer);
        Severity = diagnostic.Severity;
        SeeDetailsCommand = ReactiveCommand.Create<NavigationInformation, ValueTuple<Diagnostic, NavigationInformation>>(info => (diagnostic, info));
    }

    public Diagnostic Diagnostic { get; }
    public string Summary { get; }
    public DiagnosticSeverity Severity { get; }
    public ReactiveCommand<NavigationInformation, ValueTuple<Diagnostic, NavigationInformation>> SeeDetailsCommand { get; }
}
