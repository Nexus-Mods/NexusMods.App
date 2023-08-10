﻿using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.Generic.Installers;

/// <summary>
/// Generic mod installer for simple mods that only need to have their contents placed to a specific game location
/// (<see cref="InstallFolderTarget"/>).
/// Requires the game to support <see cref="AFolderMatchInstallerCapability"/>.
/// Tries to match the mod archive folder structure to <see cref="InstallFolderTarget"/> offered by the capability.
///
/// Example: myMod/Textures/myTexture.dds -> Skyrim/Data/Textures/myTexture.dds
/// </summary>
public class GenericFolderMatchInstaller : IModInstaller
{
    private readonly ILogger<GenericFolderMatchInstaller> _logger;


    public GameCapabilityId RequiredGameCapability => AFolderMatchInstallerCapability.CapabilityId;

    public GenericFolderMatchInstaller(ILogger<GenericFolderMatchInstaller> logger)
    {
        _logger = logger;
    }

    #region IModInstaller

    public Priority GetPriority(GameInstallation installation,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {


        if (!installation.Game.SupportedCapabilities.TryGetCapability<AFolderMatchInstallerCapability>(
                RequiredGameCapability, out var folderMatchInstallerCapability))
        {
            return Priority.None;
        }

        var installFolderTargets = folderMatchInstallerCapability!.InstallFolderTargets();

        var filePaths = archiveFiles.Keys;

        if (filePaths.Any(filePath => PathMatchesAnyTarget(filePath, installFolderTargets)))
        {
            return Priority.Normal;
        }

        return Priority.None;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation, ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles, CancellationToken cancellationToken = default)
    {
        if (!gameInstallation.Game.SupportedCapabilities.TryGetCapability<AFolderMatchInstallerCapability>(
                RequiredGameCapability, out var folderMatchInstallerCapability))
        {
            throw new NotSupportedException(
                $"Game {gameInstallation.Game.Name} does not support GenericFolderMatchInstaller capability.");
        }

        var installFolderTargets = folderMatchInstallerCapability!.InstallFolderTargets();

        List<RelativePath> missedFiles = new();

        List<FromArchive> modFiles = new();

        foreach (var target in installFolderTargets)
        {
            modFiles.AddRange(GetModFilesForTarget(archiveFiles, target, missedFiles));
            if (modFiles.Any())
            {
                // If any file matched target, ignore other targets.
                // no support for multiple targets from the same archive.
                break;
            }
        }

        if (missedFiles.Any())
        {
            // Even though installation was successful, some files were not matched to the target.
            var missedFilesString = string.Join(",\n", missedFiles);
            _logger.LogWarning("Installer could not install some files:\n {MissedFiles}", missedFilesString);
        }

        return ValueTask.FromResult<IEnumerable<ModInstallerResult>>(new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = modFiles
            }
        });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets all the mod files for a target or its sub-targets
    /// </summary>
    /// <param name="archiveFiles"></param>
    /// <param name="target"></param>
    /// <param name="missedFiles"></param>
    /// <returns></returns>
    private IEnumerable<FromArchive> GetModFilesForTarget(EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        InstallFolderTarget target, List<RelativePath> missedFiles)
    {
        List<FromArchive> modFiles = new();

        // TODO: Currently just assumes that the prefix of the first file that matches the target structure is the correct one.
        // Consider checking that each file matches the target at the found location before adding it.

        if (TryFindPrefixToDrop(target, archiveFiles.Keys, out var prefixToDrop))
        {
            foreach (var (filePath, fileData) in archiveFiles)
            {
                var trimmedPath = filePath;

                if (prefixToDrop != RelativePath.Empty)
                {
                    if (filePath.InFolder(prefixToDrop))
                    {
                        trimmedPath = filePath.RelativeTo(prefixToDrop);
                    }
                    else
                    {
                        // File didn't have the same prefix as the first file that matched the target structure.
                        // Keep track of these for debugging.
                        missedFiles.Add(filePath);
                    }
                }

                if (PathIsExcluded(trimmedPath, target))
                    continue;

                var modPath = new GamePath(target.DestinationGamePath.Type,
                    target.DestinationGamePath.Path.Join(trimmedPath));

                modFiles.Add(fileData.ToFromArchive(modPath));
            }

            return modFiles;
        }

        // No files matched, try sub targets
        foreach (var subTarget in target.SubTargets)
        {
            modFiles.AddRange(GetModFilesForTarget(archiveFiles, subTarget, missedFiles));
        }

        return modFiles;
    }

    /// <summary>
    /// If any of the files match the install target, returns true with <paramref name="prefix"/> set to the prefix to drop.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="filePaths"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    private bool TryFindPrefixToDrop(InstallFolderTarget target, IEnumerable<RelativePath> filePaths,
        out RelativePath prefix)
    {
        foreach (var filePath in filePaths)
        {
            if (PathMatchesTarget(filePath, target))
            {
                prefix = GetPrefixOfLength(filePath, GetNumParentsToDrop(filePath, target));
                return true;
            }
        }

        prefix = RelativePath.Empty;
        return false;
    }

    /// <summary>
    /// Returns the first numFolders of the path as a relative path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="numFolders"></param>
    /// <returns></returns>
    private RelativePath GetPrefixOfLength(RelativePath path, int numFolders)
    {
        // NOTE: Assumes that the path is longer than numFolders
        if (numFolders == 0)
            return RelativePath.Empty;

        var foldersToDrop = numFolders;
        var suffix = path;
        var prefix = new RelativePath("");
        while (foldersToDrop > 0)
        {
            prefix = prefix.Join(suffix.TopParent);
            suffix = suffix.DropFirst();
            foldersToDrop--;
        }

        return prefix;
    }

    /// <summary>
    /// Returns true if any file matches any target or sub-target.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="installFolderTargets"></param>
    /// <returns></returns>
    private bool PathMatchesAnyTarget(RelativePath filePath,
        IEnumerable<InstallFolderTarget> installFolderTargets)
    {
        foreach (var target in installFolderTargets)
        {
            if (PathMatchesTarget(filePath, target))
                return true;

            if (PathMatchesAnyTarget(filePath, target.SubTargets))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the path matches the target
    /// NOTE: Does not check sub-targets
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="installFolderTarget"></param>
    /// <returns></returns>
    private bool PathMatchesTarget(RelativePath filePath, InstallFolderTarget installFolderTarget)
    {
        if (PathContainsAny(filePath, installFolderTarget.KnownSourceFolderNames) > -1)
            return true;

        if (PathContainsAny(filePath, installFolderTarget.KnownValidSubfolders) > -1)
            return true;

        if (PathMatchesAnyExtensions(filePath, installFolderTarget.KnownValidFileExtensions))
            return true;

        return false;
    }

    /// <summary>
    /// Returns the number of folders that need to be dropped from the path to install the file to the target.
    /// NOTE: Assumes that the path matches the target.
    /// NOTE: number of folders to drop != depth, as depth starts from 0 for the first folder,
    ///     while number of folders to drop starts from 1.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private int GetNumParentsToDrop(RelativePath path, InstallFolderTarget target)
    {
        var depth = 0;

        if ((depth = PathContainsAny(path, target.KnownSourceFolderNames)) > -1)
        {
            // the match indicates an alias for the target, so we need the alias as well
            return depth + 1;
        }

        if ((depth = PathContainsAny(path, target.KnownValidSubfolders)) > -1)
        {
            // the match indicates a subfolder of the target, so we need to drop up to the parent
            return depth;
        }

        if (PathMatchesAnyExtensions(path, target.KnownValidFileExtensions))
        {
            // the file is a child of the target, so we need to drop everything up to the filename
            return path.Depth;
        }

        throw new InvalidOperationException($"Path {path} does not match target {target}");
    }

    /// <summary>
    /// Returns true if the path matches any of the provided extensions.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    private bool PathMatchesAnyExtensions(RelativePath filePath, IEnumerable<Extension> extensions)
    {
        foreach (var extension in extensions)
        {
            if (filePath.Extension == extension)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the install target indicates that the path should be excluded.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool PathIsExcluded(RelativePath path, InstallFolderTarget target)
    {
        if (target.SubPathsToDiscard.Any(subPath => PathContainsSubPath(path, subPath)))
            return true;
        if (PathMatchesAnyExtensions(path, target.FileExtensionsToDiscard))
            return true;

        return false;
    }

    /// <summary>
    /// Returns whether the path contains the subPath.
    ///
    /// Example: path: "skse_1_07_03/src/skse/skse.sln" subPath: "src/skse"
    /// </summary>
    /// <param name="path"></param>
    /// <param name="subPath"></param>
    /// <returns></returns>
    private bool PathContainsSubPath(RelativePath path, RelativePath subPath)
    {
        if (path.InFolder(subPath))
            return true;

        // Remove parents of path until we reach the depth of subPath
        while (path.Depth > subPath.Depth)
        {
            if (path.InFolder(subPath))
                return true;

            path = path.DropFirst();
        }

        return false;
    }

    /// <summary>
    /// If any of the folderNames are in the path, returns the depth of the first match, otherwise -1.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="folderNames"></param>
    /// <returns></returns>
    private int PathContainsAny(RelativePath path, IEnumerable<string> folderNames)
    {
        var depth = -1;
        if (folderNames.Any(folderName => (depth = GetFolderDepth(path, folderName)) > -1))
        {
            return depth;
        }

        return -1;
    }

    /// <summary>
    /// Returns the depth of the folderName in the filePath if present, otherwise -1.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="folderName"></param>
    /// <returns></returns>
    private int GetFolderDepth(RelativePath path, string folderName)
    {
        var depth = 0;
        while (path != RelativePath.Empty && path.Depth > 0)
        {
            if (path.TopParent == folderName)
                return depth;

            depth++;
            path = path.DropFirst();
        }

        return -1;
    }

    #endregion
}
