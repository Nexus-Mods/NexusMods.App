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
    private readonly StringBuilder _sb = new();
    public void Dispose() { }

    public void Write<T>(T value) where T : notnull => Write(value.ToString().AsSpan());

    public void WriteValueType<T>(T value) where T : struct => Write(value.ToString().AsSpan());

    public void Write(ReadOnlySpan<char> value) => _sb.Append(value);
    
    public string ToOutput()
    {
        var res = _sb.ToString();
        Reset();
        return res;
    }
    
    public void Reset()  => _sb.Clear();
}
