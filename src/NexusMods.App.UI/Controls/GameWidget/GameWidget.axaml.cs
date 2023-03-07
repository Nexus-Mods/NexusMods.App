using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
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
            this.Bind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                .DisposeWith(d);
            this.WhenAnyValue(view => view.ViewModel!.Image)
                .Where(img => img != null)
                .OffUI()
                .Select(img => BlurImage((IBitmap)img))
                .BindToUI(this, view => view.BlurryImage.Source)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.PrimaryButton, v => v.PrimaryButton)
                .DisposeWith(d);
        });
    }

    private IBitmap BlurImage(IBitmap image)
    {
        var ms = new MemoryStream();
        image.Save(ms);
        ms.Position = 0;
        var skiaImage = SKImage.FromEncodedData(ms);

        var skiaInfo = new SKImageInfo(skiaImage.Width, skiaImage.Height);
        var renderTarget = SKImage.Create(skiaInfo);
        var bitmap = SKBitmap.FromImage(renderTarget);

        using (var canvas = new SKCanvas(bitmap))
        {
            using (var paint = new SKPaint())
            {
                paint.ImageFilter = SKImageFilter.CreateBlur(100, 100);
                var src =
                    SKRect.Create(0, 0, skiaImage.Width, skiaImage.Height);
                //var dest = SKRect.Create(-(skiaImage.Width / 2), -(skiaImage.Height / 2), skiaImage.Width * 2, skiaImage.Height * 2);
                canvas.DrawImage(skiaImage, src, src, paint);
            }
        }

        var finalImage = new MemoryStream();
        bitmap.Encode(finalImage, SKEncodedImageFormat.Png, 100);
        finalImage.Position = 0;
        var finalIImage = new Bitmap(finalImage);
        return finalIImage;
    }
}

