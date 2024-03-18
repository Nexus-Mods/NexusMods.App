using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Values;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class NamedLinkFormatter : IValueFormatter<NamedLink>
{
    public void Format(NamedLink value, IDiagnosticWriter writer)
    {
        // TODO: markdown link
        writer.Write(value.Uri.ToString());
    }
}
