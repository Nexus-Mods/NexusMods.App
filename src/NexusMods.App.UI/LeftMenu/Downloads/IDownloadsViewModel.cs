using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public interface IDownloadsViewModel : ILeftMenuViewModel, IViewModelSelector<Options, IRightContentViewModel>
{

}
