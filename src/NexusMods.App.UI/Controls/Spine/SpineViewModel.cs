using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.LeftMenu.Game;
using NexusMods.App.UI.LeftMenu.Home;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine;

[UsedImplicitly]
public class SpineViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    public IIconButtonViewModel Home { get; }

    public ISpineDownloadButtonViewModel Downloads { get; }

    private ReadOnlyObservableCollection<IImageButtonViewModel> _loadouts = Initializers.ReadOnlyObservableCollection<IImageButtonViewModel>();
    public ReadOnlyObservableCollection<IImageButtonViewModel> Loadouts => _loadouts;

    private readonly ILogger<SpineViewModel> _logger;

    public SpineViewModel(ILogger<SpineViewModel> logger,
        ILoadoutRegistry loadoutRegistry,
        IIconButtonViewModel addButtonViewModel,
        IIconButtonViewModel homeButtonViewModel,
        ISpineDownloadButtonViewModel spineDownloadsButtonViewModel,
        IDownloadsViewModel downloadsViewModel,
        IHomeLeftMenuViewModel homeLeftMenuViewModel,
        IGameLeftMenuViewModel gameLeftMenuViewModel,
        IServiceProvider provider)
    {
        _logger = logger;

        Home = homeButtonViewModel;
        Downloads = spineDownloadsButtonViewModel;

        this.WhenActivated(disposables =>
        {
            loadoutRegistry.Loadouts
                .TransformAsync(async loadout =>
                {
                    await using var iconStream = await loadout.Installation.Game.Icon.GetStreamAsync();

                    var vm = provider.GetRequiredService<IImageButtonViewModel>();
                    vm.Name = loadout.Name;
                    vm.Image = LoadImageFromStream(iconStream);
                    vm.IsActive = false;
                    vm.Click = ReactiveCommand.Create(() => throw new NotImplementedException());
                    return vm;
                })
                .OnUI()
                .Bind(out _loadouts)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            Home.Click = ReactiveCommand.Create(NavigateToHome);

            Downloads.Click = ReactiveCommand.Create(NavigateToDownloads);

            // For now just select home on startup
            NavigateToHome();
        });
    }

    private Bitmap LoadImageFromStream(Stream iconStream)
    {
        try
        {
            return Bitmap.DecodeToWidth(iconStream, 48);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skia image load error, while loading image from stream");
            // Null images are fine, they will be ignored
            return null!;
        }
    }

    private void NavigateToHome()
    {
        _logger.LogTrace("Home selected");
        throw new NotImplementedException();
    }

    private void NavigateToGame(IGame game)
    {
        _logger.LogTrace("Game {Game} selected", game);
        throw new NotImplementedException();
    }

    private void NavigateToDownloads()
    {
        _logger.LogTrace("Downloads selected");
        throw new NotImplementedException();
    }
}
