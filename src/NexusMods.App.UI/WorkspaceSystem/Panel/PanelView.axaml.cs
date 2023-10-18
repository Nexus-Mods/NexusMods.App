using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelView : ReactiveUserControl<IPanelViewModel>
{
    public PanelView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.ActualBounds)
                .SubscribeWithErrorLogging(bounds =>
                {
                    Width = bounds.Width;
                    Height = bounds.Height;
                    Canvas.SetLeft(this, bounds.X);
                    Canvas.SetTop(this, bounds.Y);
                })
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseCommand, view => view.ClosePanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.PopoutCommand, view => view.PopOutPanelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, view => view.AddTabButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Tabs, view => view.TabContents.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TabHeaders, view => view.TabHeaders.ItemsSource)
                .DisposeWith(disposables);
        });
    }

    private void ScrollLeftButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X - 234);
    }

    private void ScrollRightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X + 234);
    }
}

