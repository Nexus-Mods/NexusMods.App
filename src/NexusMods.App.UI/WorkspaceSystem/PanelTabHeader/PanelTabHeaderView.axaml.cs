using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
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
                
            Observable
                .FromEventPattern<PointerReleasedEventArgs>(
                    addHandler => Container.PointerReleased += addHandler,
                    removeHandler => Container.PointerReleased -= removeHandler
                )
                .Select(static eventPattern => eventPattern.EventArgs)
                .Where(eventArgs => eventArgs.InitialPressMouseButton != MouseButton.Middle &&
                                    Bounds.Contains(eventArgs.GetPosition(this)))
                .Select(_ => Unit.Default)
                .InvokeReactiveCommand(this, view => view.ViewModel!.CloseTabCommand)
                .DisposeWith(disposables);
        });
    }
}
