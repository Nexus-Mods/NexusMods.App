using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Bg3Constants
{
    public static readonly Extension PakFileExtension = new(".pak");
    
    public static readonly LocationId ModsLocationId = LocationId.From("Mods");
    
    public static readonly GamePath BG3SEGamePath = new(LocationId.Game, "bin/DWrite.dll");
}
