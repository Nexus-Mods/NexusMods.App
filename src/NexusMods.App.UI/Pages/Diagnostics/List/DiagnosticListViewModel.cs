using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; }

    private ObservableAsPropertyHelper<Diagnostic[]>? _diagnostics;
    public Diagnostic[] Diagnostics => _diagnostics?.Value ?? Array.Empty<Diagnostic>();

    public DiagnosticListViewModel(
        IWindowManager windowManager,
        IDiagnosticManager diagnosticManager) : base(windowManager)
    {
        GetWorkspaceController().SetTabTitle("Diagnostics", WorkspaceId, PanelId, TabId);

        this.WhenActivated(disposable =>
        {
            var serialDisposable = new SerialDisposable();

            this.WhenAnyValue(vm => vm.LoadoutId)
                .Do(loadoutId =>
                {
                    var value = diagnosticManager
                        .GetLoadoutDiagnostics(loadoutId)
                        .ToProperty(this, vm => vm.Diagnostics, scheduler: RxApp.MainThreadScheduler);

                    _diagnostics = value;
                    serialDisposable.Disposable = value;
                })
                .Subscribe()
                .DisposeWith(disposable);

            serialDisposable.DisposeWith(disposable);
        });
    }
}
