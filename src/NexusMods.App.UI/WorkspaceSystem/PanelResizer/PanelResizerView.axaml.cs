using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelResizerView : ReactiveUserControl<IPanelResizerViewModel>
{
    /// <summary>
    /// Resizers take up the space between panels. However, since the logical are of a
    /// panel doesn't account for that, we use padding to visualize it. The Resizer must
    /// thus have a width, or height depending on the orientation, of the padding that the
    /// panels have.
    ///
    /// This padding value is multiplied by two, because resizers go between two panels.
    /// </summary>
    private const double Size = PanelView.DefaultPadding * 2;

    /// <summary>
    /// This offset is used to prevent resizers from overlapping. As such, it has to be
    /// a multiple of <see cref="Size"/>.
    /// </summary>
    private const double Offset = Size * 2.5;

    private bool _isPressed;

    private static readonly Cursor CursorHorizontal = new(StandardCursorType.SizeNorthSouth);
    private static readonly Cursor CursorVertical = new(StandardCursorType.SizeWestEast);

    public PanelResizerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.ActualPoints)
                .SubscribeWithErrorLogging(tuple =>
                {
                    if (ViewModel is null) return;

                    var (startPoint, endPoint) = tuple;
                    var isHorizontal = ViewModel.IsHorizontal;

                    if (isHorizontal)
                    {
                        Canvas.SetLeft(this, startPoint.X + Offset / 2);
                        Canvas.SetTop(this, startPoint.Y - Size / 2);
                        Width = endPoint.X - startPoint.X - Offset;
                        Height = Size;
                    }
                    else
                    {
                        Canvas.SetTop(this, startPoint.Y + Offset / 2);
                        Canvas.SetLeft(this, startPoint.X - Size / 2);
                        Height = endPoint.Y - startPoint.Y - Offset;
                        Width = Size;
                    }
                })
                .DisposeWith(disposables);

            // pressed
            Observable.FromEventPattern<PointerPressedEventArgs>(
                    addHandler: handler => PointerPressed += handler,
                    removeHandler: handler => PointerPressed -= handler)
                .Do(_ =>
                {
                    _isPressed = true;
                    DragHighlight.Opacity = 1;
                })
                .Finally(() =>
                {
                    _isPressed = false;
                })
                .Subscribe()
                .DisposeWith(disposables);

            // released
            Observable.FromEventPattern<PointerReleasedEventArgs>(
                    addHandler: handler => PointerReleased += handler,
                    removeHandler: handler => PointerReleased -= handler)
                .Do(_ =>
                {
                    _isPressed = false;
                })
                .Select(_ => Unit.Default)
                .InvokeReactiveCommand(this, view => view.ViewModel!.DragEndCommand)
                .DisposeWith(disposables);

            // drag
            Observable.FromEventPattern<PointerEventArgs>(
                    addHandler: handler => PointerMoved += handler,
                    removeHandler: handler => PointerMoved -= handler)
                .Where(_ => _isPressed)
                .Select(eventPattern =>
                {
                    if (ViewModel is null) return 0.0;

                    var parent = (Parent as Control)!;
                    var currentPos = eventPattern.EventArgs.GetPosition(parent);

                    return ViewModel.IsHorizontal ? currentPos.Y : currentPos.X;
                })
                .InvokeReactiveCommand(this, view => view.ViewModel!.DragStartCommand)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IPanelResizerViewModel viewModel)
    {
        Cursor = viewModel.IsHorizontal ? CursorHorizontal : CursorVertical;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        DragHighlight.Opacity = 1;
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (!_isPressed) DragHighlight.Opacity = 0;
        base.OnPointerExited(e);
    }
}
