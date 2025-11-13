using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Loadouts;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public interface IApplyDiffViewModel : IPageViewModelInterface
{
    public void Initialize(LoadoutId loadoutId);
    
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    
    public IViewModelInterface BodyViewModel { get; set; }
}
