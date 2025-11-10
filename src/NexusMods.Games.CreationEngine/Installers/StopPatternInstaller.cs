using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.CreationEngine.Installers;

/// <summary>
/// Installer class for Creation Engine games (Skyrim, Fallout, etc.) that handles mod installation based on file/folder patterns.
/// 
/// This installer analyzes mod archives and installs files to appropriate locations based on configured patterns:
/// - Most mod files are installed to the game's Data folder
/// - Engine files (like script extenders) are installed to the game's root folder
/// - Handles common mod packaging variations like nested Data folders or game folder aliases
/// 
/// Key features:
/// - Detects and properly handles various mod archive structures
/// - Supports game-specific file extensions (.esp, .esm, etc.)
/// - Handles script extender and engine file installations
/// - Configurable patterns for determining file destinations
/// - Supports multiple game folder name aliases
/// - Handles common mod folder structures (meshes, textures, etc.)
/// 
/// Usage:
/// 1. Configure the installer with game-specific settings (file patterns, folders, etc.)
/// 2. Call Build() to optimize the installer
/// 3. The installer will automatically handle mod installations based on the configured patterns
/// </summary>
public class StopPatternInstaller(IServiceProvider serviceProvider) : ALibraryArchiveInstaller(serviceProvider, serviceProvider.GetRequiredService<ILogger<StopPatternInstaller>>())
{
#region Customizable Properties

    /// <summary>
    /// Unique identifier for the game this installer handles.
    /// Required property that must be set when configuring the installer.
    /// </summary>
    public required GameId GameId { get; init; }
    
    /// <summary>
    /// Defines where mod data files should be installed, typically in the game's "Data" folder.
    /// Defaults to {Game}/Data directory. Used as the base installation path for most mod files.
    /// </summary>
    public GamePath DataPath { get; init; } = new(LocationId.Game, "Data");
    
    /// <summary>
    /// Defines the root game folder path where engine-related files (like script extenders) should be installed.
    /// Defaults to the base game folder. Used for files that need to be installed alongside the game executable.
    /// </summary>
    public GamePath EnginePath { get; init; } = new(LocationId.Game, RelativePath.Empty);

    /// <summary>
    /// The name of the data folder used for plugin/mod files, defaults to "Data".
    /// Used to detect and handle mods that may have nested "Data/Data" folder structures.
    /// </summary>
    public string DataFolderName { get; init; } = "Data";
    
    /// <summary>
    /// Array of common folder names used for the game installation.
    /// Required property listing known alternative names for the game folder (e.g. ["Skyrim Special Edition", "SkyrimSE"]).
    /// Used to properly handle mods packaged with different game folder names.
    /// </summary>
    public required string[] GameAliases { get; init; }
    
    /// <summary>
    /// Array of folder names that should be installed directly in the game's data folder.
    /// Required property listing directories like "meshes", "textures", etc.
    /// When these folders are found at the root of a mod, their contents are installed to Data/.
    /// </summary>
    public required string[] TopLevelDirs { get; init; }
    
    /// <summary>
    /// Array of regex patterns identifying folders that should be installed to the data directory.
    /// Required property used to match folder names that should trigger installation to the Data folder.
    /// Supplements TopLevelDirs with more complex pattern matching.
    /// </summary>
    public required string[] StopPatterns { get; init; }
    
    /// <summary>
    /// Array of regex patterns identifying files that should be installed to the game's root folder.
    /// Required property used to match engine files like script extenders.
    /// Files matching these patterns are installed to the base game folder instead of the Data folder.
    /// </summary>
    public required string[] EngineFiles { get; init; }
    
    /// <summary>
    /// Array of file extensions that indicate plugin files.
    /// Defaults to [.esm, .esp, .esl] for Creation Engine games.
    /// Used to identify and properly handle plugin files during installation.
    /// </summary>
    public Extension[] PluginLikeExtensions { get; init; } = [KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL];
    
    /// <summary>
    /// Array of file extensions that indicate archive files.
    /// Defaults to [.bsa, .ba2] for Creation Engine games.
    /// Used to identify and properly handle archive files during installation.
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


    /// <summary>
    /// Optimizes the installer given the settings, must be called before the installer can be used.
    /// </summary>
    public StopPatternInstaller Build()
    {
        _dataPrefix = RelativePath.FromUnsanitizedInput(DataFolderName);
        _gameFolders = GameAliases.Select(g => RelativePath.FromUnsanitizedInput(g)).ToArray();
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

            var targetBase = _engineFiles.IsMatch(rel.ToString()) ? EnginePath : DataPath;

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
