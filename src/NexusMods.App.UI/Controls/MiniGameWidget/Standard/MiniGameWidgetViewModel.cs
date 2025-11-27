using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Helpers;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using R3;

namespace NexusMods.App.UI.Controls.MiniGameWidget.Standard;

public class MiniGameWidgetViewModel : AViewModel<IMiniGameWidgetViewModel>, IMiniGameWidgetViewModel
{
    private static readonly Uri MissingGamesUri = new("https://nexus-mods.github.io/NexusMods.App/users/games/CompatibleGames/");

    public MiniGameWidgetViewModel(ILogger<MiniGameWidgetViewModel> logger, IOSInterop osInterop)
    {
        _image = this
            .WhenAnyValue(vm => vm.Game)
            .Where(game => game is not null)
            .OffUi()
            .ToObservable()
            .SelectAwait((game, _) => ImageHelper.LoadGameIconAsync(game, (int)ImageSizes.GameThumbnail.Width, logger))
            .AsSystemObservable()
            .WhereNotNull()
            .ToProperty(this, vm => vm.Image, scheduler: RxApp.MainThreadScheduler);

        GiveFeedbackCommand = ReactiveUI.ReactiveCommand.Create(() => osInterop.OpenUri(MissingGamesUri));

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
    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> GiveFeedbackCommand { get; }

    private readonly ObservableAsPropertyHelper<Bitmap> _image;
}
