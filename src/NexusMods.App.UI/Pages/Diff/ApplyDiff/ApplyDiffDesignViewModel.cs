using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffDesignViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    public ApplyDiffDesignViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }
    
    public ApplyDiffDesignViewModel() : base(new DesignWindowManager())
    {
    }

    public void Initialize(LoadoutId loadoutId)
    {
        throw new NotImplementedException();
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; } = ReactiveCommand.Create(() => { });
    public IViewModelInterface BodyViewModel { get; set; } = new DummyLoadingViewModel();
}
