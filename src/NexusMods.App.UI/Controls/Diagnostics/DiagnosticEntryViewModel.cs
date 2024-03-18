using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryViewModel : AViewModel<IDiagnosticEntryViewModel>, IDiagnosticEntryViewModel
{
    
    public DiagnosticEntryViewModel(Diagnostic diagnostic, IDiagnosticWriter writer)
    {
        // Obtain plain text version of the diagnostic summary
        diagnostic.FormatSummary(writer);
        Summary = writer.ToOutput();
        
        Severity = diagnostic.Severity;
        SeeDetailsCommand = ReactiveCommand.Create(() => { });
    }

    public string Summary { get; }
    public DiagnosticSeverity Severity { get; }
    public ReactiveCommand<Unit, Unit> SeeDetailsCommand { get; }
}
