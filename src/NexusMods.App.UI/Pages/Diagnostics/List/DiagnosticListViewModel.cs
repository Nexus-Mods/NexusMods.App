using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; }

    private ObservableAsPropertyHelper<IDiagnosticEntryViewModel[]>? _diagnosticEntries;
    public IDiagnosticEntryViewModel[] DiagnosticEntries => _diagnosticEntries?.Value ?? Array.Empty<IDiagnosticEntryViewModel>();

    public DiagnosticListViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IDiagnosticManager diagnosticManager) : base(windowManager)
    {
        this.WhenActivated(disposable =>
        {
            GetWorkspaceController().SetTabTitle("Diagnostics", WorkspaceId, PanelId, TabId);

            var serialDisposable = new SerialDisposable();

            this.WhenAnyValue(vm => vm.LoadoutId)
                .Do(loadoutId =>
                {
                    var value = diagnosticManager
                        .GetLoadoutDiagnostics(loadoutId)
                        .Select(diagnostics => diagnostics
                            .Select(diagnostic => new DiagnosticEntryViewModel(diagnostic, serviceProvider.GetRequiredService<IDiagnosticWriter>()))
                            .ToArray()
                        )
                        .ToProperty(this, vm => vm.DiagnosticEntries, scheduler: RxApp.MainThreadScheduler);

                    _diagnosticEntries = value;
                    serialDisposable.Disposable = value;
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposable);

            serialDisposable.DisposeWith(disposable);
        });
    }
}
