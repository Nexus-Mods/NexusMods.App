using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryDesignViewModel : DiagnosticEntryViewModel
{
    public DiagnosticEntryDesignViewModel() : base(_data) { }
    
    private static Diagnostic _data =
        new()
        {
            Id = new DiagnosticId(),
            Severity = DiagnosticSeverity.Warning,
            Summary = DiagnosticMessage.From("This is an example diagnostic."),
            Details = DiagnosticMessage.DefaultValue,
            DataReferences = new Dictionary<DataReferenceDescription, IDataReference>(),
        };
}
