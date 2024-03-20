using System.Text;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;

namespace NexusMods.App.UI.Controls.Diagnostics;

public class DiagnosticEntryDesignViewModel : DiagnosticEntryViewModel
{
    public DiagnosticEntryDesignViewModel() : base(_data, Writer) { }
    
    private static Diagnostic _data =
        new()
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
    public void Write<T>(StringBuilder stringBuilder, T value) where T : notnull
    {
        Write(stringBuilder, value.ToString().AsSpan());
    }

    public void WriteValueType<T>(StringBuilder stringBuilder, T value) where T : struct
    {
        Write(stringBuilder, value.ToString().AsSpan());
    }

    public void Write(StringBuilder stringBuilder, ReadOnlySpan<char> value)
    {
        stringBuilder.Append(value);
    }
}
