using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.Downloads;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public class DownloadsViewModel : AViewModel<IDownloadsViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public DownloadsViewModel(IServiceProvider serviceProvider)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.InProgressTitleTextBlock, Icon = IconType.None, Activate = ReactiveCommand.Create(() =>
                {
                    RightContent = serviceProvider.GetRequiredService<IInProgressViewModel>();
                })
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
