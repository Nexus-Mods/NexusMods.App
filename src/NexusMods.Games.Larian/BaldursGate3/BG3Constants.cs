using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Bg3Constants
{
    public static readonly Extension PakFileExtension = new(".pak");
    
    public static readonly LocationId ModsLocationId = LocationId.From("Mods");

    public static readonly LocationId PlayerProfilesLocationId = LocationId.From("PlayerProfiles");

    public static readonly LocationId ScriptExtenderConfigLocationId = LocationId.From("ScriptExtenderConfig");

    public static readonly GamePath BG3SEGamePath = new(LocationId.Game, "bin/DWrite.dll");
}
