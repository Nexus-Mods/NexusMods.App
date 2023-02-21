using Vogen;

namespace NexusMods.DataModel.Games;

[ValueObject<string>]
[Instance("Unknown", "unknown")]
[Instance("Steam", "steam")]
[Instance("Gog", "gog")]
[Instance("Epic", "epic")]
[Instance("Origin", "origin")]
[Instance("EADesktop", "eadesktop")]
public partial class GameStore
{
}