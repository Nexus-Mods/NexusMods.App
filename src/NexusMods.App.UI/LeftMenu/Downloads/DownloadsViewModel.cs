using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public class DownloadsViewModel : AViewModel<IDownloadsViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    public DownloadsViewModel(IServiceProvider serviceProvider)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock, Icon = IconType.None, Activate = ReactiveCommand.Create(() => throw new NotImplementedException("Navigate to workspace"))
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
