using System.Reactive;
using System.Reactive.Disposables;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    public string Details { get; }
    public DiagnosticSeverity Severity { get; }

    public ReactiveCommand<string, Unit> MarkdownOpenLinkCommand { get; }

    public DiagnosticDetailsViewModel(
        IOSInterop osInterop,
        IWindowManager windowManager,
        IDiagnosticWriter diagnosticWriter, 
        Diagnostic diagnostic) : base(windowManager)
    {
        Severity = diagnostic.Severity;

        var summary = diagnostic.FormatSummary(diagnosticWriter);
        Details = $"## {summary}\n" +
                  $"{diagnostic.FormatDetails(diagnosticWriter)}";

        // TODO: once we have custom elements and goto-links, this should be factored out into a singleton handler
        MarkdownOpenLinkCommand = ReactiveCommand.CreateFromTask<string>(async url =>
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            await Task.Run(() =>
            {
                osInterop.OpenUrl(uri);
            });
        });

        this.WhenActivated(disposable =>
        {
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetTabTitle(diagnostic.Title, WorkspaceId, PanelId, TabId);
                workspaceController.SetIcon(IconValues.DiagnosticPage, WorkspaceId, PanelId, TabId);
            }

            Disposable.Empty.DisposeWith(disposable);
        });
    }
}
