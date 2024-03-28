using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using SkiaSharp;

namespace NexusMods.App.UI.Controls.GameWidget;

public partial class GameWidget : ReactiveUserControl<IGameWidgetViewModel>
{
    private GameWidgetState _previousState;

    public GameWidget()
    {
        InitializeComponent();
        this.WhenActivated(d =>
            {
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Select(state => state == GameWidgetState.DetectedGame)
                    .BindToView(this, view => view.DetectedGameStackPanel.IsVisible)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Select(state => state == GameWidgetState.AddingGame)
                    .BindToView(this, view => view.AddingGameStackPanel.IsVisible)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Select(state => state == GameWidgetState.ManagedGame)
                    .BindToView(this, view => view.ManagedGameStackPanel.IsVisible)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Select(state => state == GameWidgetState.RemovingGame)
                    .BindToView(this, view => view.RemovingGameStackPanel.IsVisible)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Select(state => state is GameWidgetState.AddingGame or GameWidgetState.RemovingGame)
                    .BindToClasses(GameWidgetBorder, "Disabled")
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.Image)
                    .WhereNotNull()
                    .OffUi()
                    .Select(BlurAvaloniaImage)
                    .OnUI()
                    .BindToView(this, view => view.BlurryImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.PrimaryButton, v => v.AddGameButton)
                    .DisposeWith(d);
                
            }
        );
    }

    private static Bitmap BlurAvaloniaImage(Bitmap bitmap)
    {
        using var inputSkBitmap = bitmap.ToSkiaBitmap();
        using var outputSkBitmap = new SKBitmap(inputSkBitmap.Info, SKBitmapAllocFlags.ZeroPixels);

        using (var skCanvas = new SKCanvas(outputSkBitmap))
        using (var skPaint = new SKPaint())
        {
            skPaint.ImageFilter = SKImageFilter.CreateBlur(
                sigmaX: 100f,
                sigmaY: 100f
            );

            var skRect = inputSkBitmap.Info.Rect;
            skCanvas.DrawBitmap(inputSkBitmap, skRect, skPaint);
        }

        return outputSkBitmap.ToAvaloniaImage();
    }
}
