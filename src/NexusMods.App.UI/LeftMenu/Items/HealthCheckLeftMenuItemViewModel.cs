using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class HealthCheckLeftMenuItemViewModel : LeftMenuItemViewModel
{
    [Reactive] public bool IsSuggestionVisible { get; private set; }
    [Reactive] public bool IsWarningVisible { get; private set; }
    [Reactive] public bool IsCriticalVisible { get; private set; }

    public HealthCheckLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        IDiagnosticManager diagnosticManager,
        LoadoutId loadoutId) : base(workspaceController, workspaceId, pageData)
    {
        var healthCheckCountsObservable = diagnosticManager
            .CountDiagnostics(loadoutId)
            .OnUI()
            .Do(counts =>
                {
                    IsCriticalVisible = counts.NumCritical != 0;
                    IsWarningVisible = counts.NumWarnings != 0 && !IsCriticalVisible;
                    IsSuggestionVisible = counts.NumSuggestions != 0 && !IsCriticalVisible && !IsWarningVisible;
                }
            );

        this.WhenActivated(d =>
            {
                healthCheckCountsObservable.Subscribe()
                    .DisposeWith(d);
            }
        );
    }
}
