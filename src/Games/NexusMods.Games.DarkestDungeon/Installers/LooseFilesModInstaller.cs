using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.DarkestDungeon.Installers;

/// <summary>
/// <see cref="IModInstaller"/> implementation for loose file mods.
/// </summary>
public class LooseFilesModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "mods".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var files = info.ArchiveFiles
            .GetFiles()
            .Select(kv => kv.ToStoredFile(
                new GamePath(LocationId.Game, ModsFolder.Join(kv.Path()))
            ));

        // TODO: create project.xml file for the mod
        // this needs to be serialized to XML and added to the files enumerable
        // ReSharper disable once UnusedVariable
        var modProject = new ModProject
        {
            Title = info.ArchiveFiles.Path().TopParent.ToString()
        };

        return new [] { new ModInstallerResult
        {
            Id = info.BaseModId,
            Files = files.AsEnumerable(),
        }};
    }
}
