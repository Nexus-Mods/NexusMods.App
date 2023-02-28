using Vogen;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// revision number (aka "version") of a revision. Only unique within one collection
/// </summary>
[ValueObject<ulong>]
public partial struct RevisionNumber
{
    
}