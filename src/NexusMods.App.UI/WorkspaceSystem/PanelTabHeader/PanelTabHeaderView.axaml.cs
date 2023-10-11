using System.Reactive.Disposables;
using Avalonia.Input;
using Avalonia.Media;
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
        Background = Brushes.Transparent;

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Icon, view => view.IconImage.Source)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Title, view => view.TitleTextBlock.Text)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseTabCommand, view => view.CloseTabButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.ViewModel!.IsSelected)
                .SubscribeWithErrorLogging(isSelected =>
                {
                    Background = isSelected ? Brushes.Aqua : Brushes.Transparent;
                })
                .DisposeWith(disposables);
        });
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.IsSelected = true;
    }
}

