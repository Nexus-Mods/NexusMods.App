using System.Diagnostics;
using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.External.UI;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

public sealed partial class LauncherManagerNexusMods : LauncherManagerHandler
{
    public LauncherManagerNexusMods(
        GameInstallationContextAccessor gameInstallationContextAccessor,
        ILauncherStateProvider launcherProvider,
        IGameInfoProvider gameInfoProvider,
        ILoadOrderPersistenceProvider loadOrderPersistenceProvider,
        IFileSystemProvider fileSystemProvider,
        IDialogProvider dialogUIProvider,
        INotificationProvider notificationUIProvider,
        ILoadOrderStateProvider loadOrderStateProvider) : base(
        launcherProvider,
        gameInfoProvider,
        loadOrderPersistenceProvider,
        fileSystemProvider,
        dialogUIProvider,
        notificationUIProvider,
        loadOrderStateProvider)
    {
        var installation = gameInstallationContextAccessor.GameInstalltionContext ?? throw new UnreachableException();
        var store = Converter.ToGameStoreTW(installation.GameStore);
        SetGameStore(store);
    }
}
