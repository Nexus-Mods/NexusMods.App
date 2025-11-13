using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.AdvancedInstaller.Exceptions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// DeploymentData encapsulates all the data needed for a manual or advanced mod installation.
/// It contains a mapping between the source files in the archive to the target paths in the game directory.
/// </summary>
/// <remarks>
///     Note that this class only contains reference types, so it is safe to pass around by value.
///     If you are adding any value types here, you might want to re-evaluate how this struct is used in the codebase.
/// </remarks>
public readonly struct DeploymentData
{
    /// <summary>
    /// This Dictionary maps relative paths of files in the mod archive to relative paths in the game directories.<br/>
    ///
    /// Key: The relative path of a file within the mod archive.
    /// Value: The relative path where the file should be placed in the game directory.
    /// </summary>
    /// <example>
    /// If a mod file 'texture.dds' in the archive should go to 'Game/Data/Textures/texture.dds',
    /// then the KeyValuePair would be like ("texture.dds", "Game/Data/Textures/texture.dds").
    /// </example>
    /// <remarks>
    /// Paths follow internal Nexus Mods App path standards: they use a "/" as a separator, trim whitespace, and do not alter "..".
    /// </remarks>
    public Dictionary<RelativePath, GamePath> ArchiveToOutputMap { get; init; } = new();

    /// <summary>
    /// This is a reverse lookup for the _archiveToOutputMap.
    /// We use this lookup to ensure that a file has not already been mapped to a given location.
    /// </summary>
    public Dictionary<GamePath, RelativePath> OutputToArchiveMap { get; init; } = new();

    /// <summary>
    /// Public/Default Constructor.
    /// </summary>
    public DeploymentData() { }

    /// <summary>
    /// Adds a new mapping from a source file in the archive to a target path in the game directory.
    /// </summary>
    /// <param name="archivePath">The relative path of the source file within the mod archive.</param>
    /// <param name="outputPath">The relative path where the file should be placed in one of the game directories.</param>
    /// <param name="force">
    ///     If this is set to true, re-mapping the item is permitted, and will simply unmap existing item.
    ///     Otherwise <see cref="MappingAlreadyExistsException"/> will be thrown.
    /// </param>
    /// <returns>True if the mapping was added successfully, false if the key already exists.</returns>
    /// <exception cref="MappingAlreadyExistsException">If the mapping already exists, unless <see cref="force"/> is specified.</exception>
    public void AddMapping(RelativePath archivePath, GamePath outputPath, bool force = false)
    {
        // Check if said location is already mapped.
        if (OutputToArchiveMap.TryGetValue(outputPath, out var existing))
        {
            // Already mapped to same path, so it's a no-op.
            if (existing.Equals(archivePath))
                return;

            // Otherwise we throw telling which path we're mapped to.
            if (!force)
                ThrowHelpers.MappingAlreadyExists(outputPath, existing, archivePath);
            else
                RemoveMapping(existing);
        }

        ArchiveToOutputMap[archivePath] = outputPath;
        OutputToArchiveMap[outputPath] = archivePath;
    }

    /// <summary>
    /// Removes a mapping based on the source file's relative path in the archive.
    /// </summary>
    /// <param name="archivePath">The relative path of the source file within the mod archive.</param>
    /// <returns>True if the mapping was removed successfully, false if the key does not exist.</returns>
    public bool RemoveMapping(RelativePath archivePath)
    {
        var result = ArchiveToOutputMap.Remove(archivePath, out var existing);
        if (result)
            OutputToArchiveMap.Remove(existing);

        return result;
    }

    /// <summary>
    /// Clears all the existing mappings.
    /// </summary>
    public void ClearMappings()
    {
        ArchiveToOutputMap.Clear();
        OutputToArchiveMap.Clear();
    }

    public void CreateLoadoutItems(
        KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles,
        LoadoutId loadoutId,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction)
    {
        foreach (var (src, value) in ArchiveToOutputMap)
        {
            var file = archiveFiles.FindByPathFromChild(src);
            if (file is null) throw new KeyNotFoundException($"Unable to find file `{src}` in tree!");

            file.ToLoadoutFile(loadoutId, loadoutGroup, transaction, value);
        }
    }
}
