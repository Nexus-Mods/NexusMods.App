using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Installers.Trees;
using NexusMods.BCL.Extensions;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.DarkestDungeon.Installers;

/// <summary>
/// <see cref="IModInstaller"/> implementation for native Darkest Dungeon mods with
/// <c>project.xml</c> files.
/// </summary>
public class NativeModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "mods".ToRelativePath();
    private static readonly RelativePath ProjectFile = "project.xml".ToRelativePath();

    internal static async Task<IEnumerable<(KeyedBox<RelativePath, ModFileTree> Node, ModProject Project)>>
        GetModProjects(KeyedBox<RelativePath, ModFileTree> archiveFiles)
    {
        return await archiveFiles
            .GetFiles()
            .SelectAsync(async kv =>
            {
                if (kv.Path().FileName != ProjectFile)
                    return default;

                await using var stream = await kv.Item.OpenAsync();
                using var reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    IgnoreComments = true,
                    IgnoreWhitespace = true,
                    ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes
                });

                var obj = new XmlSerializer(typeof(ModProject)).Deserialize(reader);
                return (kv, obj as ModProject);
            })
            .Where(r => r.Item2 is not null)
            .Select(r => (r.kv, r.Item2!))
            .ToArrayAsync();
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modProjectFiles = (await GetModProjects(info.ArchiveFiles)).ToArray();
        if (!modProjectFiles.Any())
            return Array.Empty<ModInstallerResult>();

        if (modProjectFiles.Length > 1)
            return Array.Empty<ModInstallerResult>();

        var mods = modProjectFiles
            .Select(modProjectFile =>
            {
                var parent = modProjectFile.Node.Parent()!;
                var modProject = modProjectFile.Project;

                if (modProject is null) throw new UnreachableException();

                var modFiles = parent.GetFiles()
                    .Select(kv => kv.ToStoredFile(
                        new GamePath(LocationId.Game, ModsFolder.Join(kv.Path().DropFirst(parent.Depth() - 1)))
                    ));

                return new ModInstallerResult
                {
                    Id = ModId.NewId(),
                    Files = modFiles,
                    Name = string.IsNullOrEmpty(modProject.Title) ? null : modProject.Title,
                    Version = modProject.VersionMajor == 0
                        ? null
                        : $"{modProject.VersionMajor}.{modProject.VersionMinor}"
                };
            });

        return mods;
    }
}
