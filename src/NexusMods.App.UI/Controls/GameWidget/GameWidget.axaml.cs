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
                    .Subscribe(state =>
                    {
                        DetectedGameStackPanel.IsVisible = state == GameWidgetState.DetectedGame;
                        AddingGameStackPanel.IsVisible = state == GameWidgetState.AddingGame;
                        ManagedGameStackPanel.IsVisible = state == GameWidgetState.ManagedGame;
                        RemovingGameStackPanel.IsVisible = state == GameWidgetState.RemovingGame;
                        GameWidgetBorder.Classes.ToggleIf("Disabled", state is GameWidgetState.AddingGame or GameWidgetState.RemovingGame);
                    })
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

                this.BindCommand(ViewModel, vm => vm.AddGameCommand, v => v.AddGameButton)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.ViewGameCommand, v => v.ViewGameButton)
                    .DisposeWith(d);
                
                // Adding a game is same as adding a loadout for now
                this.BindCommand(ViewModel, vm => vm.AddGameCommand, v => v.AddLoadoutButton)
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
