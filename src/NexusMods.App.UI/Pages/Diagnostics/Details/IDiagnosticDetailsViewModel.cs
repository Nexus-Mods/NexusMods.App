using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diagnostics;

public interface IDiagnosticDetailsViewModel : IPageViewModelInterface
{
    DiagnosticSeverity Severity { get; }

    IMarkdownRendererViewModel MarkdownRendererViewModel { get; }
}
