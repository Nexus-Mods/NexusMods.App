using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk;
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
    public IViewModelInterface BodyViewModel { get; set; } = null!;
}
