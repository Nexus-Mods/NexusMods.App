using System.Xml;
using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

// The NexusMods.App should be able to use a custom Load Order storage, like Vortex
internal sealed class LoadOrderPersistenceProvider : ILoadOrderPersistenceProvider
{
    private readonly IOptionsSnapshot<MountAndBlade2BannerlordOptions> _options;
    private readonly ILauncherStateProvider _launcherStateProvider;
    private readonly UserDataProvider _userDataProvider;

    public LoadOrderPersistenceProvider(IOptionsSnapshot<MountAndBlade2BannerlordOptions> options, ILauncherStateProvider launcherStateProvider, UserDataProvider userDataProvider)
    {
        _options = options;
        _launcherStateProvider = launcherStateProvider;
        _userDataProvider = userDataProvider;
    }

    public LoadOrder LoadLoadOrder()
    {
        var userData = _userDataProvider.LoadUserData();
        if (userData is null) return new LoadOrder();

        var state = _launcherStateProvider.GetState();

        var userGameTypeData = state.IsSingleplayer ? userData.SingleplayerData : userData.MultiplayerData;
        return new LoadOrder(userGameTypeData.ModDatas.Select((x, i) => new LoadOrderEntry(x.Id, string.Empty, x.IsSelected, i)));
    }

    // We try to keep as much original game data as possible, especially useful when they add new data
    // or when custom launchers want to add their own data
    // Be aware that this is not the best place to keep custom settings, because the vanilla launcher
    // will delete them when launched. So only store non important options or duplicate them via NMA.
    // It is recommended to still persist here settings that are used by other launchers (like BLSE)
    public void SaveLoadOrder(LoadOrder loadOrder)
    {
        var state = _launcherStateProvider.GetState();
        var options = _launcherStateProvider.GetOptions();

        var oldUserData = _userDataProvider.LoadUserData();
        var oldUserGameTypeData = state.IsSingleplayer ? oldUserData?.SingleplayerData : oldUserData?.MultiplayerData ?? UserGameTypeData.Empty;

        var modDatas = new List<UserModData>();
        foreach (var (id, entry) in loadOrder)
        {
            var oldModData = oldUserGameTypeData?.ModDatas.FirstOrDefault(x => x.Id == id);
            modDatas.Add(new UserModData
            {
                Id = id,
                IsSelected = entry.IsSelected,
                Nodes = oldModData?.Nodes ?? Array.Empty<XmlNode>(),
            });
        }

        var userGameTypeData = new UserGameTypeData
        {
            ModDatas = modDatas,
            Nodes = oldUserGameTypeData?.Nodes ?? Array.Empty<XmlNode>(),
        };

        var userData = new UserData
        {
            GameType = state.IsSingleplayer ? GameType.Singleplayer : GameType.Multiplayer,
            SingleplayerData = state.IsSingleplayer ? userGameTypeData : oldUserData?.SingleplayerData ?? UserGameTypeData.Empty,
            MultiplayerData = !state.IsSingleplayer ? userGameTypeData : oldUserData?.MultiplayerData ?? UserGameTypeData.Empty,
            FixCommonIssues = options.FixCommonIssues,
            BetaSorting = options.BetaSorting,
            DisableBinaryCheck = _options.Value.DisableBinaryCheck,
            DisableCrashHandlerWhenDebuggerIsAttached = _options.Value.DisableCrashHandlerWhenDebuggerIsAttached,
            DisableCatchAutoGenExceptions = _options.Value.DisableCatchAutoGenExceptions,
            UseVanillaCrashHandler = _options.Value.UseVanillaCrashHandler,
            Nodes = oldUserGameTypeData?.Nodes ?? Array.Empty<XmlNode>(),
        };
        _userDataProvider.SaveUserData(userData);
    }
}
