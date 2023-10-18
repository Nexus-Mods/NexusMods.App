using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelView : ReactiveUserControl<IPanelViewModel>
{
    private const double ScrollOffset = 250;

    public PanelView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(
                    view => view.TabHeaderScrollViewer.Extent,
                    view => view.TabHeaderScrollViewer.Viewport,
                    (extent, viewport) => extent.Width > viewport.Width)
                .SubscribeWithErrorLogging(isVisible =>
                {
                    ScrollLeftButton.IsVisible = isVisible;
                    ScrollRightButton.IsVisible = isVisible;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(
                    view => view.TabHeaderScrollViewer.ScrollBarMaximum,
                    view => view.TabHeaderScrollViewer.Offset)
                .SubscribeWithErrorLogging(tuple =>
                {
                    var (offset, scrollBarMaximum) = tuple;
                    ScrollLeftButton.IsEnabled = offset.X < ScrollOffset;
                    ScrollRightButton.IsEnabled = !offset.X.IsCloseTo(scrollBarMaximum.X);
                })
                .DisposeWith(disposables);

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
        Console.WriteLine(nameof(ScrollLeftButton_OnClick));
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X - ScrollOffset);
    }

    private void ScrollRightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine(nameof(ScrollRightButton_OnClick));
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X + ScrollOffset);
    }
}

