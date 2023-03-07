using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    private readonly ILogger<GameWidgetViewModel> _logger;

    public GameWidgetViewModel(ILogger<GameWidgetViewModel> logger)
    {
        _logger = logger;

        PrimaryButton = ReactiveCommand.Create(() => { });
        SecondaryButton = ReactiveCommand.Create(() => { });

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Installation)
                .OffUI()
                .SelectMany(async install => await LoadImage(install))
                .Where(img => img != null)
                .OnUI()
                .BindToUI(this, vm => vm.Image)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Installation)
                .Select(inst => $"{inst.Game.Name} v{inst.Version}")
                .BindToUI(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }

    private async Task<IImage?> LoadImage(GameInstallation source)
    {
        try
        {
            var stream = await source.Game.GameImage.GetStreamAsync();
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "While loading game image for {GameName} v{Version}", source.Game.Name, source.Version);
            return null;
        }
    }

    [Reactive]
    public GameInstallation Installation { get; set; }

    [Reactive]
    public string Name { get; set; }

    [Reactive]
    public IImage Image { get; set; }

    [Reactive]
    public ICommand PrimaryButton { get; set; }

    [Reactive]
    public ICommand? SecondaryButton { get; set; }
}
