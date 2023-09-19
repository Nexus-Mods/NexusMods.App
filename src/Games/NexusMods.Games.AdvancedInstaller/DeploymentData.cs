using System.ComponentModel;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// DeploymentData encapsulates all the data needed for a manual or advanced mod installation.
/// It contains a mapping between the source files in the archive to the target paths in the game directory.
/// </summary>
public struct DeploymentData
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
    internal Dictionary<RelativePath, GamePath> ArchiveToOutputMap { get; init; } = new();

    /// <summary>
    /// This is a reverse lookup for the _archiveToOutputMap.<br/>.
    /// We use this lookup to ensure that a file has not already been mapped to a given location.
    /// </summary>
    internal Dictionary<GamePath, RelativePath> OutputToArchiveMap { get; init; } = new();

    /// <summary>
    /// Public/Default Constructor.
    /// </summary>
    public DeploymentData() { }

    /// <summary>
    /// Adds a new mapping from a source file in the archive to a target path in the game directory.
    /// </summary>
    /// <param name="archivePath">The relative path of the source file within the mod archive.</param>
    /// <param name="outputPath">The relative path where the file should be placed in one of the game directories.</param>
    /// <returns>True if the mapping was added successfully, false if the key already exists.</returns>
    public void AddMapping(RelativePath archivePath, GamePath outputPath)
    {
        // Check if said location is already mapped.
        if (OutputToArchiveMap.TryGetValue(outputPath, out var existing))
        {
            // Already mapped to same path, so it's a no-op.
            if (existing.Equals(archivePath))
                return;

            // Otherwise we throw telling which path we're mapped to.
            ThrowHelpers.MappingAlreadyExists(outputPath, existing, archivePath);
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

    /*
    /// <summary>
    /// Emits a series of AModFile instructions based on the current mappings.
    /// </summary>
    /// <param name="gameTargetPath">Path to the game folder.</param>
    /// <param name="archiveFiles">Files from the archive.</param>
    /// <returns>An IEnumerable of AModFile, representing the files to be moved and their target paths.</returns>
    public IEnumerable<AModFile> EmitOperations(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GamePath gameTargetPath)
    {
        // Written like this for clarity, use array in actual code.
        // Just an example, might not compile.
        foreach (var mapping in _archiveToOutputMap)
        {
            // find file in `files` input.
            var file = files.First(file => file.Key.Equals(RelativePath.FromUnsanitizedInput(mapping.Key)));

            yield return new FromArchive
            {
                Id = ModId.New(),
                To = new GamePath(gameTargetPath.Type,
                    gameTargetPath.Path.Join(RelativePath.FromUnsanitizedInput(mapping.Value))),
                Hash = file.Value.Hash,
                Size = file.Value.Size
            };
        }
    }
    */
}
