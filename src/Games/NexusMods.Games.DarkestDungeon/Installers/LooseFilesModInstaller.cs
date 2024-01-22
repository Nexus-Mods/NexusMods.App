using Cathei.LinqGen;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.DarkestDungeon.Installers;

/// <summary>
/// <see cref="IModInstaller"/> implementation for loose file mods.
/// </summary>
public class LooseFilesModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "mods".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var files = archiveFiles
            .GetFiles()
            .Select(kv => kv.ToStoredFile(
                new GamePath(LocationId.Game, ModsFolder.Join(kv.Path()))
            ));

        // TODO: create project.xml file for the mod
        // this needs to be serialized to XML and added to the files enumerable
        // ReSharper disable once UnusedVariable
        var modProject = new ModProject
        {
            Title = archiveFiles.Path().TopParent.ToString()
        };

        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = files.AsEnumerable(),
        }};
    }
}
