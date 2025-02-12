namespace NexusMods.Games.FileHashes.DTOs;

/// <summary>
/// A contributed definition for a version
/// </summary>
public class VersionContribDefinition
{
    /// <summary>
    /// The human-friendly name of the version
    /// </summary>
    public required string Name { get; set; } 

    /// <summary>
    /// Gog build ids for this version
    /// </summary>
    public string[] GOG { get; set; } = [];
    
    /// <summary>
    /// Steam manifest ids for this version
    /// </summary>
    public string[] Steam { get; set; } = [];
}


