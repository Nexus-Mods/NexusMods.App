using NexusMods.App.UI.RightContent.Downloads;
using NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public enum Options
{
    [ViewModel<IInProgressViewModel>]
    InProgress,
    [ViewModel<ICompletedViewModel>]
    Completed,
    [ViewModel<IHistoryViewModel>]
    History
}
