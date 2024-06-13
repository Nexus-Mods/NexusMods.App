using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class NewTabPageView : ReactiveUserControl<INewTabPageViewModel>
{
    public NewTabPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.BannerSettingsWrapper, view => view.InfoBanner.BannerSettingsWrapper)
                .DisposeWith(disposable);

            this.OneWayBind(ViewModel, vm => vm.StateIcon, view => view.AddPanelIcon.Value)
                .DisposeWith(disposable);

            this.OneWayBind(ViewModel, vm => vm.Sections, view => view.Sections.ItemsSource)
                .DisposeWith(disposable);
        });
    }
}
