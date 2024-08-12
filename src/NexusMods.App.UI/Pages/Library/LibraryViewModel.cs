using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.Library;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;

    public GameDomain GameDomain { get; }

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = loadout.InstallationInstance.Game;

        GameDomain = game.Domain;
    }
}
