namespace NexusMods.Abstractions.GOG.Values;

/// <summary>
/// Enum representing the type of depot, for now we only care about depots, but GOG also allows for
/// patches, which can be used to upgrade from one game version to another. 
/// </summary>
public enum DepotItemType
{
    /// <summary>
    /// The depot contains files
    /// </summary>
    /// <returns></returns>
    DepotFile
}
