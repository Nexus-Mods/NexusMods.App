using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    private readonly ILogger<GameWidgetViewModel> _logger;

    public GameWidgetViewModel(ILogger<GameWidgetViewModel> logger, ISettingsManager settingsManager, IFileHashesService fileHashesService)
    {
        _logger = logger;

        AddGameCommand = ReactiveCommand.Create(() => { });
        ViewGameCommand = ReactiveCommand.Create(() => { });
        RemoveAllLoadoutsCommand = ReactiveCommand.Create(() => { });

        _image = this
            .WhenAnyValue(vm => vm.Installation)
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
                    .SelectMany(async installation =>
                    {
                        await fileHashesService.GetFileHashesDb();
                        var locatorIds = installation.LocatorResultMetadata?.ToLocatorIds().ToArray() ?? [];
                        if (!fileHashesService.TryGetVanityVersion((installation.Store, locatorIds), out var vanityVersion))
                            return "Version: Unknown";
                        return $"Version: {vanityVersion.Value}";
                    })
                    .BindToVM(this, vm => vm.Version)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.Installation)
                    .Select(inst => $"{inst.Store.Value}")
                    .BindToVM(this, vm => vm.Store)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.Installation)
                    .Select(inst => MapGameStoreToIcon(inst.Store))
                    .BindToVM(this, vm => vm.GameStoreIcon)
                    .DisposeWith(disposables);
                
                IsManagedObservable
                    .Select(v => v ? GameWidgetState.ManagedGame : GameWidgetState.DetectedGame)
                    .OnUI()
                    .BindToVM(this, vm => vm.State)
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
            _logger.LogError(ex, "While loading game image for {GameName}", source.Game.Name);
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
    [Reactive] public string Version { get; set; } = "";
    [Reactive] public string Store { get; set; } = "";
    public IconValue GameStoreIcon { get; set; } = new IconValue();

    private readonly ObservableAsPropertyHelper<Bitmap> _image;
    public Bitmap Image => _image.Value;

    [Reactive] public ReactiveCommand<Unit, Unit> AddGameCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, Unit> RemoveAllLoadoutsCommand { get; set; }
    
    public IObservable<bool> IsManagedObservable { get; set; } = Observable.Return(false);


    [Reactive] public GameWidgetState State { get; set; }
}
