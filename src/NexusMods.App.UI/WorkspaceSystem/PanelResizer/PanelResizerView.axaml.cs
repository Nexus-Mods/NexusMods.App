using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelResizerView : ReactiveUserControl<IPanelResizerViewModel>
{
    private const double Size = PanelView.DefaultPadding * 2;
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

            this.WhenAnyValue(
                    view => view.ViewModel!.ActualStartPoint,
                    view => view.ViewModel!.ActualEndPoint)
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
                    Icon.IsVisible = true;
                })
                .Finally(() =>
                {
                    _isPressed = false;
                    Icon.IsVisible = false;
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
                    Icon.IsVisible = false;
                })
                .Select(_ => Unit.Default)
                .InvokeCommand(this, view => view.ViewModel!.DragEndCommand)
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
                .InvokeCommand(this, view => view.ViewModel!.DragStartCommand)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IPanelResizerViewModel viewModel)
    {
        Cursor = viewModel.IsHorizontal ? CursorHorizontal : CursorVertical;
        Icon.Classes.Add(viewModel.IsHorizontal ? "DragHorizontal" : "DragVertical");
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        Icon.IsVisible = true;
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (!_isPressed) Icon.IsVisible = false;
        base.OnPointerExited(e);
    }
}
