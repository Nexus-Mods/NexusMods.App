using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Games.Abstractions;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon;

public class DarkestDungeon : AGame,  ISteamGame, IGogGame
{
    public IEnumerable<int> SteamIds => new[] { 262060 };
    public IEnumerable<long> GogIds => new long[] { 1450711444 };

    public DarkestDungeon(ILogger<DarkestDungeon> logger, IEnumerable<IGameLocator> gameLocators) : base(logger, gameLocators)
    {
    }

    public override string Name => "Darkest Dungeon";
    public override string Slug => "darkestdungeon";
    public override GamePath PrimaryFile
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new(GameFolderType.Game, @"_linuxnosteam\darkest.bin.x86_64");
            }
            return new(GameFolderType.Game, @"_windowsnosteam\Darkest.exe");
        }
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }


}