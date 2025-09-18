using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MiniGameWidget.Standard;

public class MiniGameWidgetViewModel : AViewModel<IMiniGameWidgetViewModel>, IMiniGameWidgetViewModel
{
    private readonly ILogger<MiniGameWidgetViewModel> _logger;
    private const string MissingGamesUrl = "https://nexus-mods.github.io/NexusMods.App/users/games/CompatibleGames/";

    public MiniGameWidgetViewModel(ILogger<MiniGameWidgetViewModel> logger, 
        IOSInterop osInterop,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        
        _image = this
            .WhenAnyValue(vm => vm.Game)
            .Where(game => game is not null)
            .OffUi()
            .SelectMany(game => ImageHelper.LoadGameIconAsync(game, (int)ImageSizes.GameThumbnail.Width, _logger))
            .WhereNotNull()
            .ToProperty(this, vm => vm.Image, scheduler: RxApp.MainThreadScheduler);

        GiveFeedbackCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(new Uri(MissingGamesUrl)); });

        this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(vm => vm.Name)
                    .BindToVM(this, vm => vm.Name)
                    .DisposeWith(disposables);

                _image.DisposeWith(disposables);
            }
        );
    }



    [Reactive] public IGame? Game { get; set; }
    public GameInstallation[]? GameInstallations { get; set; }
    [Reactive] public string Name { get; set; } = "";
    public bool IsFound { get; set; }
    public Bitmap Image => _image.Value;
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }

    private readonly ObservableAsPropertyHelper<Bitmap> _image;
}
