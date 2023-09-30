using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public class DownloadsViewModel : ViewModelSelector<Options, IRightContentViewModel, IDownloadsViewModel>, IDownloadsViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; } =
        Initializers.ReadOnlyObservableCollection<ILeftMenuItemViewModel>();

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } =
        Initializers.IRightContent;
    
    public DownloadsViewModel(IServiceProvider provider) : base(provider)
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.CurrentViewModel)
                .BindTo(this, vm => vm.RightContent)
                .DisposeWith(d);
        });
    }
}
