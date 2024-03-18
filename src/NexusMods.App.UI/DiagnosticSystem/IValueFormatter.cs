using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.DiagnosticSystem;

[PublicAPI]
public interface IValueFormatter
{
    void Format(object value, IDiagnosticWriter writer);
}

[PublicAPI]
public interface IValueFormatter<in T> : IValueFormatter where T : notnull
{
    void IValueFormatter.Format(object value, IDiagnosticWriter writer)
    {
        if (value is not T actualValue) throw new NotImplementedException();
        Format(actualValue, writer);
    }

    void Format(T value, IDiagnosticWriter writer);
}
