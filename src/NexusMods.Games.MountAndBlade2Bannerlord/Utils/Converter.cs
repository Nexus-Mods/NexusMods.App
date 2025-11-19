using GameStore = NexusMods.Sdk.Games.GameStore;
using GameStoreTW = Bannerlord.LauncherManager.Models.GameStore;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Utils;

public static class Converter
{
    public static GameStoreTW ToGameStoreTW(GameStore store)
    {
        if (store == GameStore.Steam)
            return GameStoreTW.Steam;
        if (store == GameStore.GOG)
            return GameStoreTW.GOG;
        if (store == GameStore.EGS)
            return GameStoreTW.Epic;
        if (store == GameStore.XboxGamePass)
            return GameStoreTW.Xbox;
        return GameStoreTW.Unknown;
    }
}
