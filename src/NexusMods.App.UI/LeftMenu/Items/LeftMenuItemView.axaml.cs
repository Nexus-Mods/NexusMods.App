using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LeftMenuItemView : ReactiveUserControl<ILeftMenuItemViewModel>
{
    public LeftMenuItemView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Text.Value.Value, view => view.LabelTextBlock.Text)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.ToolTipText)
                    .Subscribe(text => ToolTip.SetTip(this, text))
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Icon, view => view.LeftIcon.Value)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.NavigateCommand, view => view.NavButton)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsActive, view => view.NavButton.IsActive)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsSelected, view => view.NavButton.IsSelected)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel)
                    .Subscribe(vm =>
                        {
                            RightContentControl.DataContext = vm;
                            RightContentControl.Content = vm;
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}
