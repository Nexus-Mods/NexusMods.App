using System.Reactive.Disposables;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    public DiagnosticSeverity Severity { get; }

    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public DiagnosticDetailsViewModel(
        IWindowManager windowManager,
        IDiagnosticWriter diagnosticWriter,
        IMarkdownRendererViewModel markdownRendererViewModel,
        Diagnostic diagnostic) : base(windowManager)
    {
        TabIcon = IconValues.Cardiology;
        TabTitle = diagnostic.Title;
        Severity = diagnostic.Severity;

        var summary = diagnostic.FormatSummary(diagnosticWriter);
        var details = $"## {summary}\n" +
                  $"{diagnostic.FormatDetails(diagnosticWriter)}";

        MarkdownRendererViewModel = markdownRendererViewModel;
        MarkdownRendererViewModel.Contents = details;
    }
}
