using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

public sealed partial class LauncherManagerNexusMods : LauncherManagerHandler,
    IGameInfoProvider,
    ILauncherStateProvider,
    ILoadOrderPersistenceProvider,
    ILoadOrderStateProvider
{
    private readonly ILogger _logger;
    private readonly string _installationPath;


    public string ExecutableParameters { get; private set; } = string.Empty;

    public LauncherManagerNexusMods(IServiceProvider serviceProvider, string installationPath, GameStore store)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<LauncherManagerNexusMods>>();
        _installationPath = installationPath;

        Initialize(this,
            this,
            this,
            serviceProvider.GetRequiredService<FileSystemProvider>(),
            serviceProvider.GetRequiredService<DialogProvider>(),
            serviceProvider.GetRequiredService<NotificationProvider>(),
            this
        );
        SetGameStore(store);
    }
    

    string IGameInfoProvider.GetInstallPath() => _installationPath;

    public void SetGameParameters(string executable, IReadOnlyList<string> gameParameters) => ExecutableParameters = string.Join(" ", gameParameters);
    LauncherOptions ILauncherStateProvider.GetOptions() => null!;
    LauncherState ILauncherStateProvider.GetState() => null!;


    LoadOrder ILoadOrderPersistenceProvider.LoadLoadOrder() => null!;
    void ILoadOrderPersistenceProvider.SaveLoadOrder(LoadOrder loadOrder) { }

    IModuleViewModel[]? ILoadOrderStateProvider.GetAllModuleViewModels() => null;

    IModuleViewModel[]? ILoadOrderStateProvider.GetModuleViewModels() => null;

    void ILoadOrderStateProvider.SetModuleViewModels(IReadOnlyList<IModuleViewModel> moduleViewModels) { }
}
