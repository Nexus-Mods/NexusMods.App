using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Values;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class NamedLinkFormatter : IValueFormatter<NamedLink>
{
    public void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, NamedLink value)
    {
        // TODO: markdown link
        writer.Write(ref state, value.Uri.ToString());
    }
}
