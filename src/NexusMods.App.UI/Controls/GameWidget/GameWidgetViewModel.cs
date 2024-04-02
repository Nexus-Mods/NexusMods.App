using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    private readonly ILogger<GameWidgetViewModel> _logger;

    public GameWidgetViewModel(ILogger<GameWidgetViewModel> logger)
    {
        _logger = logger;

        AddGameCommand = ReactiveCommand.Create(() => { });
        ViewGameCommand = ReactiveCommand.Create(() => { });

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
                .Select(inst => $"{inst.Game.Name} v{inst.Version}")
                .BindToVM(this, vm => vm.Name)
                .DisposeWith(disposables);

            _image.DisposeWith(disposables);
        });
    }

    private async Task<Bitmap?> LoadImage(GameInstallation source)
    {
        try
        {
            var stream = await ((IGame)source.Game).GameImage.GetStreamAsync();
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "While loading game image for {GameName} v{Version}", source.Game.Name, source.Version);
            return null;
        }
    }

    [Reactive]
    public GameInstallation Installation { get; set; } = GameInstallation.Empty;

    [Reactive] public string Name { get; set; } = "";

    private readonly ObservableAsPropertyHelper<Bitmap> _image;
    public Bitmap Image => _image.Value;

    [Reactive]
    public ReactiveCommand<Unit,Unit> AddGameCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; }


    [Reactive]
    public GameWidgetState State { get; set; }
}
