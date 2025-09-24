using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.Settings;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

/// <summary>
/// The entry point for integration with the common `LauncherManager` library
/// which shares the common logic used to manage Bannerlord within a multiple set of mod managers.
///
/// This class serves as a marshaller, translating between the `LauncherManager` dependency and
/// the 'Nexus Mods App' logic.
/// </summary>
public sealed partial class LauncherManagerNexusModsApp : LauncherManagerHandler,
    IGameInfoProvider,
    ILauncherStateProvider,
    ILoadOrderStateProvider
{
    private readonly ILogger _logger;
    private readonly BannerlordSettings _settings;
    private readonly string _installationPath;

    public string[] ExecutableParameters { get; private set; } = [];

    public LauncherManagerNexusModsApp(IServiceProvider serviceProvider, string installationPath, GameStore store)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<LauncherManagerNexusModsApp>>();
        
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<BannerlordSettings>();

        _installationPath = installationPath;
        
        Initialize(this,
            this,
            serviceProvider.GetRequiredService<FileSystemProvider>(),
            serviceProvider.GetRequiredService<DialogProvider>(),
            serviceProvider.GetRequiredService<NotificationProvider>(),
            this
        );
        SetGameStore(store);
    }
    

    string IGameInfoProvider.GetInstallPath() => _installationPath;

    /// <summary>
    /// This sets the commandline arguments for the game, arguments include:
    /// - Current Game Mode
    /// - Current Load Order
    /// - Current Save File (/continuesave flag)
    /// - Whether to auto-load last save file. (/continuegame flag)
    /// </summary>
    /// <param name="executable">The game executable.</param>
    /// <param name="gameParameters">List of commandline arguments.</param>
    public void SetGameParameters(string executable, IReadOnlyList<string> gameParameters) => ExecutableParameters = gameParameters.ToArray();
    
    /// <summary>
    /// Allows us to modify the settings for the vanilla Bannerlord Launcher.
    /// </summary>
    LauncherOptions ILauncherStateProvider.GetOptions() => new()
    {
        BetaSorting = _settings.BetaSorting,
    };
    
    /// <summary>
    /// This allows us for control over booting in SP/MP mode.
    /// Bannerlord has mods which are separately available in SP and MP; which mostly
    /// acts as a filter of which mods to show. We can use the preference here (in the future)
    /// to boot directly into SP/MP mode, skipping the standard mod manager/launcher
    /// if desired by the user.
    /// </summary>
    LauncherState ILauncherStateProvider.GetState() => null!;
    
    /// <summary>
    /// Returns all available ViewModels (all mods in loadout)
    /// </summary>
    /// <remarks>
    ///     We do not need to set the <see cref="IModuleViewModel.Index"/> field, this is
    ///     set by the LauncherManager library itself.
    /// </remarks>
    IModuleViewModel[]? ILoadOrderStateProvider.GetAllModuleViewModels() => null;

    /// <summary>
    /// Returns the current shown sorted ViewModels, based on MnemonicDB state.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    ///     We do not need to set the <see cref="IModuleViewModel.Index"/> field, this is
    ///     set by the LauncherManager library itself.
    ///
    ///     Performing a <see cref="LauncherManagerHandler.Sort"/> to reset the sort to a 'default' state
    ///     may do a call back into <see cref="ILoadOrderStateProvider.SetModuleViewModels"/>
    /// </remarks>
    IModuleViewModel[]? ILoadOrderStateProvider.GetModuleViewModels() => null;

    /// <summary>
    /// Sets the current shown sorted ViewModels.
    /// This is called into when 'autosort' i.e. <see cref="LauncherManagerHandler.Sort"/> is performed,
    /// setting the sort to a 'default' state.
    /// </summary>
    void ILoadOrderStateProvider.SetModuleViewModels(IReadOnlyList<IModuleViewModel> moduleViewModels)
    {
        // TODO: The set of 'enabled' mods may have changed.
        // The set of 'index'es may have changed.
        // We need to diff against our state in the DB, and update the DB accordingly.
    }
    
    /*
    Future notes (Sewer):

        1. Bannerlord.ModuleManager.ValidateLoadOrder can be used to verify whether the user
           just tried moving an item into a valid position.

        2. `LauncherManagerHandler.TryOrderByLoadOrder` function can be used to:
            - Set final sort order passed to game.
            - Auto 'fix' invalid positions of items in load order.

        3. `LauncherManagerHandler.Sort` can be used for an `Auto-sort` feature, if user wants to reset load order.
            - Note: This calls into `SetModuleViewModels`.
            
        4. For importing enabled mods (& SortOrder) from existing setups on initial ingest,
           use `ModuleListHandler.Import`. This will import a load order (ordered list of enabled mods).
           The mods in that list will be enabled, order will be set, and `SetModuleViewModels`
           will be called.
           
           Existing items will not 'vanish', they will be imported.
    */
}
