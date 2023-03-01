using System.Reactive.Disposables;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.RightContent;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Type = NexusMods.App.UI.Controls.Spine.Type;

namespace NexusMods.App.UI.ViewModels;

public class MainWindowViewModel : AViewModel
{
    private readonly AViewModel _homeViewModel;

    public MainWindowViewModel(SpineViewModel spineViewModel, FoundGamesViewModel foundGamesViewModel)
    {
        Spine = spineViewModel;
        _homeViewModel = foundGamesViewModel;
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
    public AViewModel RightContent { get; set; }
}
