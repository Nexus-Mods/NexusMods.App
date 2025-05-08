using System.Text.RegularExpressions;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// A pattern for searching for dependant and dependency mods, the dependant mod is a mod that requires a given dependency to operate.
/// </summary>
public record Pattern
{
    /// <summary>
    /// A name for the required mod. Does not need to be unique across other patterns.
    /// </summary>
    public required string DependencyName { get; init; }
    
    /// <summary>
    /// A documentation string that explains why we think the mod is required.
    /// </summary>
    public required string Explanation { get; init; }
    
    /// <summary>
    /// The NexusMods mod ID of the required mod.
    /// </summary>
    public required ModId ModId { get; init; }
    
    /// <summary>
    /// Search patterns for files that are used to find mods that may require the dependency. These are considerd
    /// as 'OR' conditions, meaning that if any of the patterns match, the mod is considered dependant.
    /// </summary>
    public required DependantSearchPattern[] DependantSearchPatterns { get; init; }
    
    /// <summary>
    /// Paths to the dependency files. If all the paths exist, the dependency is considered installed. These are
    /// considered as 'AND' conditions, meaning that all paths must exist for the dependency to be considered installed.
    /// </summary>
    public required GamePath[] DependencyPaths { get; init; }
    
}

/// <summary>
/// A pattern for searching for dependant mods, the dependant mod is a mod that requires a given dependency to operate.
/// Fore example, ArchiveXL from Cyberpunk2077 requires Red4Ext to be installed. In this case, ArchiveXL is the dependant mod
/// and Red4Ext is the dependency.
/// </summary>
public record DependantSearchPattern
{
    /// <summary>
    /// Base search paths for the dependant mod
    /// </summary>
    public required GamePath Path { get; init; }
    
    /// <summary>
    /// File extension to search for in the path
    /// </summary>
    public required Extension Extension { get; init; }
    
    /// <summary>
    /// If provided, will perform a regex search on the file contents, assuming the file is a text file.
    /// </summary>
    public Optional<Regex> Regex { get; init; }
}
