using System.Reactive.Disposables;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.RightContent;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Type = NexusMods.App.UI.Controls.Spine.Type;

namespace NexusMods.App.UI.ViewModels;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>
{
    private readonly IViewModel _homeViewModel;

    public MainWindowViewModel(ISpineViewModel spineViewModel, FoundGamesViewModel foundGamesViewModel, ITopBarViewModel topBarViewModel)
    {
        Spine = spineViewModel;
        _homeViewModel = foundGamesViewModel;
        TopBarViewModel = topBarViewModel;
        this.WhenActivated(disposables =>
        {
            Spine.Actions
                .Subscribe(HandleSpineAction)
                .DisposeWith(disposables);
        });
    }

    private void HandleSpineAction(SpineButtonAction action)
    {
        if (action.Type == Type.Home)
        {
            RightContent = _homeViewModel;
        }

        Spine.Activations.OnNext(action);
    }

    [Reactive]
    public ISpineViewModel Spine { get; set; }

    [Reactive]
    public IViewModel RightContent { get; set; }

    [Reactive]
    public ITopBarViewModel TopBarViewModel { get; set; }
}
