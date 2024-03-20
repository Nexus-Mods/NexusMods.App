using System.Text;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Values;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class NamedLinkFormatter : IValueFormatter<NamedLink>
{
    public void Format(IDiagnosticWriter writer, StringBuilder stringBuilder, NamedLink value)
    {
        // TODO: markdown link
        writer.Write(stringBuilder, value.Uri.ToString());
    }
}
