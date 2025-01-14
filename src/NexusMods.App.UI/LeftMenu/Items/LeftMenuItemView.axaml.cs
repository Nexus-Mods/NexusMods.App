using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LeftMenuItemView : ReactiveUserControl<INewLeftMenuItemViewModel>
{
    public LeftMenuItemView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                
                this.OneWayBind(ViewModel, vm => vm.Text, view => view.LabelTextBlock.Text)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.Icon, view => view.LeftIcon.Value)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.NavigateCommand, view => view.NavButton)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.IsActive, view => view.NavButton.IsActive)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.IsSelected, view => view.NavButton.IsSelected)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.IsToggleVisible, view => view.ToggleSwitch.IsVisible)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.ToggleSwitch.IsEnabled)
                    .DisposeWith(d);
            }
        );
    }
}

