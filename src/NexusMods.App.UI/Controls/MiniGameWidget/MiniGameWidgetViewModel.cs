using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.Icons;
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
            .WhenAnyValue(vm => vm.Installation)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            .Where(installation => installation.Game is not null)
            .OffUi()
            .SelectMany(LoadImage)
            .WhereNotNull()
            .ToProperty(this, vm => vm.Image, scheduler: RxApp.MainThreadScheduler);

        this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(vm => vm.Installation)
                    .Select(inst => $"{inst.Game.Name}")
                    .BindToVM(this, vm => vm.Name)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.Installation)
                    .Select(inst => $"{inst.Store.Value}")
                    .BindToVM(this, vm => vm.Store)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.Installation)
                    .Select(inst => MapGameStoreToIcon(inst.Store))
                    .BindToVM(this, vm => vm.GameStoreIcon)
                    .DisposeWith(disposables);

                _image.DisposeWith(disposables);
            }
        );
    }

    private async Task<Bitmap?> LoadImage(GameInstallation source)
    {
        try
        {
            var stream = await ((IGame)source.Game).Icon.GetStreamAsync();
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "While loading game image for {GameName} v{Version}", source.Game.Name,
                source.Version
            );
            return null;
        }
    }

    /// <summary>
    /// Returns an <see cref="IconValue"/> for a given <see cref="Abstractions.GameLocators.GameStore"/>.
    /// </summary>
    /// <param name="store">A <see cref="Abstractions.GameLocators.GameStore"/> object</param>
    /// <returns>An <see cref="IconValue"/> icon representing the game store or a question mark icon if not found.</returns>
    internal static IconValue MapGameStoreToIcon(GameStore store)
    {
        if (store == Abstractions.GameLocators.GameStore.Steam)
            return IconValues.Steam;
        else if (store == Abstractions.GameLocators.GameStore.GOG)
            return IconValues.GOG;
        else if (store == Abstractions.GameLocators.GameStore.EGS)
            return IconValues.Epic;
        else if (store == Abstractions.GameLocators.GameStore.Origin)
            return IconValues.Ubisoft;
        else if (store == Abstractions.GameLocators.GameStore.EADesktop)
            return IconValues.EA;
        else if (store == Abstractions.GameLocators.GameStore.XboxGamePass)
            return IconValues.Xbox;

        return IconValues.Help;
    }

    [Reactive] public GameInstallation Installation { get; set; } = GameInstallation.Empty;
    [Reactive] public string Name { get; set; } = "";
    [Reactive] public string Store { get; set; } = "";
    public IconValue GameStoreIcon { get; set; } = new IconValue();

    private readonly ObservableAsPropertyHelper<Bitmap> _image;
    public Bitmap Image => _image.Value;

    [Reactive] public GameWidgetState State { get; set; }
    public bool Placeholder { get; }
}
