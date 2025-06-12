using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public class MiniGameWidgetViewModel : AViewModel<IMiniGameWidgetViewModel>, IMiniGameWidgetViewModel
{
    private readonly ILogger<MiniGameWidgetViewModel> _logger;

    public MiniGameWidgetViewModel(ILogger<MiniGameWidgetViewModel> logger, ISettingsManager settingsManager)
    {
        _logger = logger;

        _image = this
            .WhenAnyValue(vm => vm.Game)
            .Where(game => game is not null)
            .OffUi()
            .SelectMany(LoadImage)
            .WhereNotNull()
            .ToProperty(this, vm => vm.Image, scheduler: RxApp.MainThreadScheduler);

        this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(vm => vm.Name)
                    .BindToVM(this, vm => vm.Name)
                    .DisposeWith(disposables);

                _image.DisposeWith(disposables);
            }
        );
    }

    private async Task<Bitmap?> LoadImage(IGame? game)
    {
        if (game is null)
            return null;

        try
        {
            var iconStream = await game.Icon.GetStreamAsync();
            return Bitmap.DecodeToWidth(iconStream, 48);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "While loading game image for {GameName}", game.Name);
            return null;
        }
    }

    [Reactive] public IGame? Game { get; set; }
    public GameInstallation[]? GameInstallations { get; set; }
    [Reactive] public string Name { get; set; } = "";
    public bool IsFound { get; set; }
    public Bitmap Image => _image.Value;
    
    private readonly ObservableAsPropertyHelper<Bitmap> _image;

}
