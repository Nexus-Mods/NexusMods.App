using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.BaldursGate3;

public static class Bg3Constants
{
    public static readonly Extension PakFileExtension = new Extension(".pak");
    
    public static readonly LocationId ModsLocationId = LocationId.From("Mods");
}
