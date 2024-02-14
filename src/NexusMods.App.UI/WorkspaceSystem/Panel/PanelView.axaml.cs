using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelView : ReactiveUserControl<IPanelViewModel>
{
    private const double ScrollOffset = 250;
    internal const double DefaultPadding = 6.0;

    public PanelView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.LogicalBounds)
                .Do(logicalBounds =>
                {
                    var left = logicalBounds.Left.IsCloseTo(0.0) ? 0.0 : DefaultPadding;
                    var top = logicalBounds.Top.IsCloseTo(0.0) ? 0.0 : DefaultPadding;
                    var right = logicalBounds.Right.IsCloseTo(1.0) ? 0.0 : DefaultPadding;
                    var bottom = logicalBounds.Bottom.IsCloseTo(1.0) ? 0.0 : DefaultPadding;

                    Padding = new Thickness(left, top, right, bottom);
                })
                .Subscribe()
                .DisposeWith(disposables);

            // update scroll buttons and AddTab button (show left aligned or right aligned, depending on the scrollbar visibility)
            Observable.FromEventPattern<ScrollChangedEventArgs>(
                    addHandler => TabHeaderScrollViewer.ScrollChanged += addHandler,
                    removeHandler => TabHeaderScrollViewer.ScrollChanged -= removeHandler
                ).Select(_ => TabHeaderScrollViewer.Extent.Width > TabHeaderScrollViewer.Viewport.Width)
                .SubscribeWithErrorLogging(isScrollbarVisible =>
                {
                    ScrollLeftButton.IsVisible = isScrollbarVisible;
                    ScrollRightButton.IsVisible = isScrollbarVisible;

                    // the first button is inside the scroll area
                    AddTabButton1Container.IsVisible = !isScrollbarVisible;

                    // the second button is fixed on the right side
                    AddTabButton2.IsVisible = isScrollbarVisible;

                    var scrollBarMaximum = TabHeaderScrollViewer.ScrollBarMaximum;
                    var offset = TabHeaderScrollViewer.Offset;

                    ScrollLeftButton.IsEnabled = offset.X > 0;
                    ScrollRightButton.IsEnabled = !offset.X.IsCloseTo(scrollBarMaximum.X);
                })
                .DisposeWith(disposables);

            // set the bounds
            this.WhenAnyValue(view => view.ViewModel!.ActualBounds)
                .SubscribeWithErrorLogging(bounds =>
                {
                    Width = bounds.Width;
                    Height = bounds.Height;
                    Canvas.SetLeft(this, bounds.X);
                    Canvas.SetTop(this, bounds.Y);
                })
                .DisposeWith(disposables);

            // close panel button
            this.BindCommand(ViewModel, vm => vm.CloseCommand, view => view.ClosePanelButton)
                .DisposeWith(disposables);

            this.WhenAnyObservable(view => view.ViewModel!.CloseCommand.CanExecute)
                .BindToView(this, view => view.ClosePanelButton.IsVisible)
                .DisposeWith(disposables);

            // popout panel button
            this.BindCommand(ViewModel, vm => vm.PopoutCommand, view => view.PopOutPanelButton)
                .DisposeWith(disposables);

            this.WhenAnyObservable(view => view.ViewModel!.PopoutCommand.CanExecute)
                .BindToView(this, view => view.PopOutPanelButton.IsVisible)
                .DisposeWith(disposables);

            // two "add tab" buttons
            this.BindCommand(ViewModel, vm => vm.AddTabCommand, view => view.AddTabButton1)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, view => view.AddTabButton2)
                .DisposeWith(disposables);

            // tab contents and headers
            this.OneWayBind(ViewModel, vm => vm.Tabs, view => view.TabContents.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Tabs, view => view.TabHeaders.ItemsSource)
                .DisposeWith(disposables);

            // button to scroll to the left
            Observable.FromEventPattern<RoutedEventArgs>(
                    addHandler => ScrollLeftButton.Click += addHandler,
                    removeHandler => ScrollLeftButton.Click -= removeHandler
                ).Select(_ =>
                {
                    var currentOffset = TabHeaderScrollViewer.Offset;
                    return currentOffset.WithX(currentOffset.X - ScrollOffset);
                })
                .BindToView(this, view => view.TabHeaderScrollViewer.Offset)
                .DisposeWith(disposables);

            // button to scroll to the right
            Observable.FromEventPattern<RoutedEventArgs>(
                    addHandler => ScrollRightButton.Click += addHandler,
                    removeHandler => ScrollRightButton.Click -= removeHandler
                ).Select(_ =>
                {
                    var currentOffset = TabHeaderScrollViewer.Offset;
                    return currentOffset.WithX(currentOffset.X + ScrollOffset);
                })
                .BindToView(this, view => view.TabHeaderScrollViewer.Offset)
                .DisposeWith(disposables);

            // styling:
            this.WhenAnyValue(view => view.ViewModel!.Tabs.Count)
                .Select(count => count == 1)
                .Do(hasOneTab =>
                {
                    if (hasOneTab)
                    {
                        TabHeaderBorder.Classes.Add("OneTab");
                        PanelBorder.Classes.Add("OneTab");
                    }
                    else
                    {
                        TabHeaderBorder.Classes.Remove("OneTab");
                        PanelBorder.Classes.Remove("OneTab");
                    }
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);
        });
    }
}

