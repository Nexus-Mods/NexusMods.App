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

    public MainWindowViewModel(SpineViewModel spineViewModel, FoundGamesViewModel foundGamesViewModel, TopBarViewModel topBarViewModel)
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

    public SpineViewModel Spine { get; }

    [Reactive]
    public IViewModel RightContent { get; set; }

    [Reactive]
    public TopBarViewModel TopBarViewModel { get; set; }
}
