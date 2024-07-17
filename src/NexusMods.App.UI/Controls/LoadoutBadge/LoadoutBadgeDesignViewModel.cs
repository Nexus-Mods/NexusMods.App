using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public class LoadoutBadgeDesignViewModel : AViewModel<ILoadoutBadgeViewModel>, ILoadoutBadgeViewModel
{
    private enum LoadoutAppliedState
    {
        NotApplied,
        Applying,
        Applied,
    }

    private LoadoutAppliedState _loadoutAppliedState = LoadoutAppliedState.NotApplied;

    public LoadoutBadgeDesignViewModel()
    {
        this.WhenActivated(d =>
            {
                if (!Design.IsDesignMode) return;

                // Cycle thorough all the states for preview purposes
                Observable.Interval(TimeSpan.FromSeconds(0.5))
                    .Subscribe(_ => { IsLoadoutSelected = !IsLoadoutSelected; })
                    .DisposeWith(d);

                Observable.Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(_ =>
                        {
                            switch (_loadoutAppliedState)
                            {
                                case LoadoutAppliedState.NotApplied:
                                    IsLoadoutApplied = false;
                                    IsLoadoutInProgress = false;
                                    _loadoutAppliedState = LoadoutAppliedState.Applying;
                                    break;
                                case LoadoutAppliedState.Applying:
                                    IsLoadoutApplied = false;
                                    IsLoadoutInProgress = true;
                                    _loadoutAppliedState = LoadoutAppliedState.Applied;
                                    break;
                                case LoadoutAppliedState.Applied:
                                    IsLoadoutApplied = true;
                                    IsLoadoutInProgress = false;
                                    _loadoutAppliedState = LoadoutAppliedState.NotApplied;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }

    [Reactive] public string LoadoutShortName { get; set; } = "A";
    [Reactive] public bool IsLoadoutSelected { get; set; } = false;
    [Reactive] public bool IsLoadoutApplied { get; set; } = false;
    [Reactive] public bool IsLoadoutInProgress { get; set; } = false;
}
