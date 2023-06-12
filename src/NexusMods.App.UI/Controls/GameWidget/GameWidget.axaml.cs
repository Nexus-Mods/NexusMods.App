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
            this.Bind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Image)
                .WhereNotNull()
                .OffUi()
                .Select(img => BlurAvaloniaImage((Bitmap)img))
                .BindToUi(this, view => view.BlurryImage.Source)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.PrimaryButton, v => v.PrimaryButton)
                .DisposeWith(d);
        });
    }

    private Bitmap BlurAvaloniaImage(Bitmap image)
    {
        switch (image)
        {
            case WriteableBitmap writeable:
                return writeable.ToSkiaImage().BlurImage().ToAvaloniaImage();
            case { } bitmap:
                return bitmap.ToSkiaImage().BlurImage().ToAvaloniaImage();
        }

        // Slow fallback.
        var ms = new MemoryStream();
        image.Save(ms);
        ms.Position = 0;
        return SKImage.FromEncodedData(ms).BlurImage().ToAvaloniaImage();
    }
}

