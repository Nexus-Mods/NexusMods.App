using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; }

    public DiagnosticListViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }
}
