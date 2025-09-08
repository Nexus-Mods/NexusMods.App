using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Games.CreationEngine.Installers;

/// <summary>
/// This is mostly a reimplementation of the Vortex Stop Pattern Installer, once this code has enough unit tests, the code
/// can be cleaned up and improved 
/// </summary>
public class StopPatternInstaller : ALibraryArchiveInstaller
{
#region Customizable Properties

    /// <summary>
    /// The gameId this installer is for.
    /// </summary>
    public required GameId GameId { get; init; }
    
    /// <summary>
    /// The default installation folder for the game (often {Game}/Data)
    /// </summary>
    public GamePath PluginPath { get; init; } = new(LocationId.Game, "Data");
    
    /// <summary>
    /// The root folder for the game's engine files.'
    /// </summary>
    public GamePath EnginePath { get; init; } = new(LocationId.Game, RelativePath.Empty);

    /// <summary>
    /// The name of the folder that will contain the plugin files, used to detect data/data patterns in
    /// mods.
    /// </summary>
    public string PluginFolderName { get; init; } = "Data";
    
    /// <summary>
    /// Known aliases for the game folder
    /// </summary>
    public required string[] GameFolders { get; init; }
    
    /// <summary>
    /// The folder names that are considered "top level", and trigger the installer to put these folders in the plugin folder.
    /// </summary>
    public required string[] TopLevelDirs { get; init; }
    
    /// <summary>
    /// Regex patterns that match the folder names that are considered "top level". Files matching these patterns
    /// will be moved into the plugin folder and everything else under that folder will be copied as-is
    /// </summary>
    public required string[] StopPatterns { get; init; }
    
    /// <summary>
    /// Files that match these patterns will be copied into the game's root folder and the mod will be assumed to be
    /// an "engine" extension/hook library
    /// </summary>
    public required string[] EngineFiles { get; init; }
    
    /// <summary>
    /// Files with these extensions can ever go into `Data`, so we'll assume they are plugins.
    /// </summary>
    public Extension[] PluginLikeExtensions { get; init; } = [KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL];
    
    /// <summary>
    /// Files with these extensions can ever go into `Data`, so we'll assume they are archives.' 
    /// </summary>
    public Extension[] ArchiveLikeExtensions { get; init; } = [KnownCEExtensions.BSA, KnownCEExtensions.BA2];

#endregion

    private bool _isBuilt = false;
    
    // The /Data folder for the current game
    private RelativePath _dataPrefix = null!;
    private RelativePath[] _gameFolders = null!;
    private Regex _stopPatterns = null!;
    private Regex _engineFiles = null!;
    private RelativePath[] _topLevelPaths = null!;


    public StopPatternInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<FallbackInstaller>>())
    {
    }

    /// <summary>
    /// Optimizes the installer given the settings. 
    /// </summary>
    public StopPatternInstaller Build()
    {
        _dataPrefix = RelativePath.FromUnsanitizedInput(PluginFolderName);
        _gameFolders = GameFolders.Select(g => RelativePath.FromUnsanitizedInput(g)).ToArray();
        _engineFiles = CombineRegexes(EngineFiles);
        _topLevelPaths = TopLevelDirs.Select(t => RelativePath.FromUnsanitizedInput(t)).ToArray();
        _isBuilt = true;

        // Now combine all the sources of stop patterns
        
        List<string> patterns = new();
        
        foreach (var ext in PluginLikeExtensions)
            patterns.Add($"\\{ext.ToString()}$");
        
        foreach (var ext in ArchiveLikeExtensions)
            patterns.Add($"\\.{ext.ToString()}$");
        
        foreach (var dir in TopLevelDirs)
            patterns.Add($"(^|/)({dir})(/|$)");
        
        foreach (var pattern in StopPatterns)
            patterns.Add(pattern);
        
        _stopPatterns = CombineRegexes(patterns.ToArray());
        
        return this;
    }


    /// <summary>
    /// Combines regex patters into a single regex that ors them together.
    /// </summary>
    private Regex CombineRegexes(params string[] patterns)
    {
        var combined = string.Join("|", patterns.Select(p => $"(?:{p})"));
        return new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
    


    public override ValueTask<InstallerResult> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        if (!_isBuilt)
            throw new InvalidOperationException("The installer has not been built yet.");
            
        // Flatten all files
        var allFiles = libraryArchive.Children.ToArray();
        
        if (allFiles.Length == 0)
            return ValueTask.FromResult(FailWithReason("The provided library archive has no files"));

        // Compute the root prefix
        var rootPrefix = ComputeEffectiveRootPrefix(allFiles);

        foreach (var file in allFiles)
        {
            if (!file.Path.InFolder(rootPrefix))
                continue;
            
            var rel = StripLeadingData(file.Path.RelativeTo(rootPrefix));

            var targetBase = _engineFiles.IsMatch(rel.ToString()) ? EnginePath : PluginPath;

            var target = new GamePath(targetBase.LocationId, targetBase.Path / rel);
            
            var libFile = file.AsLibraryFile();
            _ = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, target.LocationId, target.Path),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = file.Path.FileName,
                        LoadoutId = loadout.Id,
                        ParentId = loadoutGroup.Id,
                    },
                },
                Hash = libFile.Hash,
                Size = libFile.Size,
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }

    private RelativePath ComputeEffectiveRootPrefix(LibraryArchiveFileEntry.ReadOnly[] files)
    {
        var firstSegments = files
            .Select(file => file.Path.TopParent)
            .Where(file => file != default(RelativePath))
            .Distinct()
            .ToArray();

        if (firstSegments.Length == 1)
        {
            var top = firstSegments[0];
            
            // Case A: The first segment is the game's root folder, step into it
            if (top == _dataPrefix)
                return top;
            
            // Case B: The first segment is a game folder, step into it
            if (_gameFolders.Contains(top))
                return top;
            
            if (_topLevelPaths.Contains(top))
                return RelativePath.Empty;
            
            // Case C: single top folder that's a generic wrapper (e.g. the mod's name)
            // If its children show any stop patterns: (esl/esp, fomod, common dirs), 
            // then step into the pointless wrapper
            if (FolderLikelyWrapper(files, top))
                return top;
            
            return RelativePath.Empty;
        }
        
        // Multiple Top Folders
        
        // Stop patterns at root
        if (HasStopPatternAtOrBelow(files, _dataPrefix))
            return RelativePath.Empty;
        
        // Data folder at the top level
        if (firstSegments.Any(p => p == _dataPrefix))
            return _dataPrefix;
            
        // Game folder at the top level
        if (firstSegments.Where(p => _gameFolders.Contains(p)).TryGetFirst(out var gameTop))
            return gameTop;
        
        return RelativePath.Empty;
    }

    /// <summary>
    /// Returns true if the folder is likely to be a wrapper for a mod.
    /// </summary>
    private bool FolderLikelyWrapper(LibraryArchiveFileEntry.ReadOnly[] files, RelativePath prefix)
    {
        foreach (var file in files)
        {
            if (TryToRelativePathBelow(file.Path, prefix, out var below) && StopMatched(below))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasStopPatternAtOrBelow(LibraryArchiveFileEntry.ReadOnly[] files, RelativePath prefix)
    {
        foreach (var file in files)
        {
            if (file.Path == default(RelativePath) || file.Path.InFolder(prefix))
            {
                if (StopMatched(file.Path))
                    return true;
                
            }
        }
        return false;
    }
    
    private RelativePath StripLeadingData(RelativePath path)
    {
        if (path.InFolder(_dataPrefix))
            return path.RelativeTo(_dataPrefix);
        return path;
    }

    /// <summary>
    /// Returns true if the file's path is below the given prefix and matches any of the stop patterns.
    /// </summary>
    private bool TryToRelativePathBelow(RelativePath filePath, RelativePath prefix, out RelativePath below)
    {
        if (!filePath.InFolder(prefix))
        {
            below = default(RelativePath);
            return false;
        }
        below = filePath.RelativeTo(prefix);
        return true;
    }

    private bool StopMatched(RelativePath path)
    {
        return _stopPatterns.IsMatch(path.ToString());
    }
}
