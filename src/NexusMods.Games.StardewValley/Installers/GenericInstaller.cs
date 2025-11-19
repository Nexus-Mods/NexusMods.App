using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// Generic installer for Stardew Valley that installs files into the "Mods" directory.
/// </summary>
public class GenericInstaller : ALibraryArchiveInstaller
{
    private readonly IFileStore _fileStore;

    public GenericInstaller(IServiceProvider serviceProvider, ILogger<GenericInstaller> logger) : base(serviceProvider, logger)
    {
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var manifestFiles = await GetManifestsAsync(libraryArchive, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        var manifestFileIds = manifestFiles.Select(static x => x.FileEntry.Id).ToHashSet();

        var previousInMods = Optional<bool>.None;

        foreach (var fileEntry in libraryArchive.Children)
        {
            var path = fileEntry.Path;
            if (path.InFolder(Constants.ContentFolder)) return new NotSupported(Reason: "The installer doesn't support putting files in the Content folder");

            var inModsFolder = path.InFolder(Constants.ModsFolder);
            if (previousInMods.HasValue)
            {
                // NOTE(erri120): this installer doesn't support this case where a mod is
                // packages to be installed into the root of the game folder
                // /foo
                // /Mods/bar
                if (previousInMods.Value != inModsFolder) return new NotSupported(Reason: "The installer doesn't support a mix of files going into the root directory and the mods directory");
            }
            else
            {
                previousInMods = inModsFolder;
            }

            var gamePath = new GamePath(LocationId.Game, inModsFolder ? path : Constants.ModsFolder.Join(path));

            var loadoutFile = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = gamePath.ToGamePathParentTuple(loadout),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.AsLibraryFile().FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup,
                    },
                },
            };

            if (!manifestFileIds.Contains(fileEntry.Id)) continue;
            _ = new SMAPIManifestLoadoutFile.New(transaction, entityId)
            {
                IsManifestFile = true,
                LoadoutFile = loadoutFile,
            };
        }

        return new Success();
    }

    private async IAsyncEnumerable<ManifestFiles> GetManifestsAsync(LibraryArchive.ReadOnly libraryArchive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var seenModDirectories = new HashSet<RelativePath>();

        // NOTE(erri120): ordered by file path depth so that top level items come before deeper nested items:
        // 1) foo/manifest
        // 2) foo/bar/manifest
        foreach (var fileEntry in libraryArchive.Children.OrderBy(x => x.Path.Depth))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            if (!fileEntry.Path.FileName.Equals(Constants.ManifestFile)) continue;
            if (seenModDirectories.Any(x => fileEntry.Path.InFolder(x))) continue;

            SMAPIManifest? manifest = null;
            try
            {
                await using var stream = await _fileStore.GetFileStream(fileEntry.AsLibraryFile().Hash, token: cancellationToken);
                manifest = await Interop.DeserializeManifest(stream, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while deserializing {Path} from {Archive}", fileEntry.Path, fileEntry.Parent.AsLibraryFile().FileName);
            }

            if (manifest is null) continue;

            seenModDirectories.Add(fileEntry.Path.Parent);
            yield return new ManifestFiles(manifest, fileEntry);
        }
    }

    private record struct ManifestFiles(
        SMAPIManifest Manifest,
        LibraryArchiveFileEntry.ReadOnly FileEntry
    );
}
