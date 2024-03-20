using System.Reactive.Disposables;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    public string Details { get; }
    public DiagnosticSeverity Severity { get; }

    public DiagnosticDetailsViewModel(IWindowManager windowManager, 
        IDiagnosticWriter diagnosticWriter, 
        Diagnostic diagnostic) : base(windowManager)
    {
        Severity = diagnostic.Severity;
        Details = diagnostic.FormatDetails(diagnosticWriter);

        this.WhenActivated(disposable =>
        {
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetTabTitle(diagnostic.Title, WorkspaceId, PanelId, TabId);
                workspaceController.SetIcon(DiagnosticIcons.DiagnosticIcon2, WorkspaceId, PanelId, TabId);
            }

            Disposable.Empty.DisposeWith(disposable);
        });
    }
}
