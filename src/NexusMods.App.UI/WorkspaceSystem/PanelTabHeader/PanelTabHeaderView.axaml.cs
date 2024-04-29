using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class PanelTabHeaderView : ReactiveUserControl<IPanelTabHeaderViewModel>
{
    public PanelTabHeaderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Icon)
                .Do(icon =>
                {
                    Icon.IsVisible = !icon.Value.IsT0;
                })
                .BindToView(this, view => view.Icon.Value)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Title, view => view.TitleTextBlock.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.CanClose, view => view.CloseTabButton.IsVisible)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.Title)
                .Subscribe(title =>
                {
                    ToolTip.SetTip(this, title);
                    ToolTip.SetShowDelay(this, (int)TimeSpan.FromMilliseconds(500).TotalMilliseconds);
                })
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseTabCommand, view => view.CloseTabButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.ViewModel!.IsSelected)
                .Subscribe(isSelected =>
                {
                    if (isSelected) Container.Classes.Add("Selected");
                    else Container.Classes.Remove("Selected");
                })
                .DisposeWith(disposables);

            Observable.FromEventPattern<PointerPressedEventArgs>(
                    addHandler => Container.PointerPressed += addHandler,
                    removeHandler => Container.PointerPressed -= removeHandler
                ).Select(_ => true)
                .BindToView(this, view => view.ViewModel!.IsSelected)
                .DisposeWith(disposables);
        });
    }
}
