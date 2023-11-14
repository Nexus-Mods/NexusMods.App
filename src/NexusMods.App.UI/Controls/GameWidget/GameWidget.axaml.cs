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
    public GameWidget()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
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

            this.BindCommand(ViewModel, vm => vm.PrimaryButton, v => v.PrimaryButton)
                .DisposeWith(d);
        });
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

