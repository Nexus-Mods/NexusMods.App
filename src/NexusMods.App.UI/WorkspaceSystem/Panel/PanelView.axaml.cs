using System.Reactive.Disposables;
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
                .SubscribeWithErrorLogging(isScrollbarVisible =>
                {
                    ScrollLeftButton.IsVisible = isScrollbarVisible;
                    ScrollRightButton.IsVisible = isScrollbarVisible;

                    AddTabButton1.IsVisible = !isScrollbarVisible;
                    AddTabButton2.IsVisible = isScrollbarVisible;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(
                    view => view.TabHeaderScrollViewer.ScrollBarMaximum,
                    view => view.TabHeaderScrollViewer.Offset)
                .SubscribeWithErrorLogging(tuple =>
                {
                    var (scrollBarMaximum, offset) = tuple;
                    ScrollLeftButton.IsEnabled = offset.X > 0;
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

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, view => view.AddTabButton1)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, view => view.AddTabButton2)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Tabs, view => view.TabContents.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Tabs, view => view.TabHeaders.ItemsSource)
                .DisposeWith(disposables);
        });
    }

    private void ScrollLeftButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X - ScrollOffset);
    }

    private void ScrollRightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var currentOffset = TabHeaderScrollViewer.Offset;
        TabHeaderScrollViewer.Offset = currentOffset.WithX(currentOffset.X + ScrollOffset);
    }
}

