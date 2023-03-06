using Vogen;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Unique ID for a game file hosted on a mod page.
/// 
/// This ID is unique within the context of the game.
/// i.e. This ID might be used for another mod if you search for mods for another game.
/// </summary>
[ValueObject<ulong>]
public partial struct FileId
{

}
