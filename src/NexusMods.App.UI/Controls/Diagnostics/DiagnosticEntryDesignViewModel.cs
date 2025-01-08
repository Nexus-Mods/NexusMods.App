using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryDesignViewModel : DiagnosticEntryViewModel
{
    public DiagnosticEntryDesignViewModel() : base(Data, Writer) { }
    
    public DiagnosticEntryDesignViewModel(Diagnostic diagnostic) : base(diagnostic, Writer) { }

    private static readonly Diagnostic Data = new()
    {
        Id = new DiagnosticId(),
        Title = "This is an example diagnostic Title",
        Severity = DiagnosticSeverity.Warning,
        Summary = DiagnosticMessage.From("This is an example diagnostic summary"),
        Details = DiagnosticMessage.DefaultValue,
        DataReferences = new Dictionary<DataReferenceDescription, IDataReference>(),
    };
 
    private static readonly DummyDiagnosticWriter Writer = new();
}

internal sealed class DummyDiagnosticWriter : IDiagnosticWriter
{
    public void Write<T>(ref DiagnosticWriterState state, T value) where T : notnull
    {
        Write(ref state, value.ToString().AsSpan());
    }

    public void WriteValueType<T>(ref DiagnosticWriterState state, T value) where T : struct
    {
        Write(ref state, value.ToString().AsSpan());
    }

    public void Write(ref DiagnosticWriterState state, ReadOnlySpan<char> value)
    {
        state.StringBuilder.Append(value);
    }
}
