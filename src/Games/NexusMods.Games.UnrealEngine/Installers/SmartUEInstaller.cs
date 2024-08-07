using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;


namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class SmartUEInstaller : ALibraryArchiveInstaller
{
    private readonly IConnection _connection;

    public SmartUEInstaller(ILogger<SmartUEInstaller> logger, IConnection connection, IServiceProvider serviceProvider) : base(serviceProvider, logger)
    {
        _connection = connection;
    }

    /// <summary>
    /// A collextion of <see cref="Regex"/>es to try an parse Archive filename.
    /// </summary>
    private static IEnumerable<Regex> ModArchiveNameRegexes =>
    [
        Constants.DefaultUEModArchiveNameRegex(),
        Constants.ModArchiveNameRegexFallback(),
    ];

    // This is here for reference, to be removed when loadout items reach parity with this implementation
    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        // TODO: add support for executable files
        // TODO: add support for config ini files, preferably merge and not replace
        // TODO: test with mod downloaded with metadata, i.e. via website
        // TODO: see what can be done with IDs extracted from archive filename

        var gameFolderPath = info.Locations[LocationId.Game];
        var gameMainUEFolderPath = info.Locations[Constants.GameMainUE];

        var achiveFiles = info.ArchiveFiles.GetFiles();

        if (achiveFiles.Length == 0)
        {
            Logger.LogError("Archive contains 0 files");
            return [];
        }

        var loadout = Loadout.All(_connection.Db)
            .First(x => x.Installation.Path == gameFolderPath.ToString());

        var gameFilesLookup = loadout.Mods
            .First(mod => mod.Category == ModCategory.GameFiles).Files
            .Select(file => file.To).ToLookup(x => x.Path.FileName);

        var modFiles = achiveFiles.Select(kv =>
        {
            switch (kv.Path().Extension)
            {
                case Extension ext when Constants.ContentExts.Contains(ext):
                    {
                        var matchesGameContentFles = gameFilesLookup[kv.FileName()];
                        if (matchesGameContentFles.Any()) // if Content file exists in game dir replace it
                        {
                            var matchedContentFIle = matchesGameContentFles.First();
                            return kv.ToStoredFile(
                                    matchedContentFIle
                                );
                        }
                        else
                            return kv.ToStoredFile(
                                new GamePath(Constants.GameMainUE, Constants.ContentModsPath.Join(kv.FileName()))
                            );
                    }
                case Extension ext when ext == Constants.DLLExt:
                    {
                        var matchesGameDlls = gameFilesLookup[kv.FileName()];
                        if (matchesGameDlls.Any()) // if DLL exists in game dir replace it
                        {
                            var matchedDll = matchesGameDlls.First();
                            return kv.ToStoredFile(
                                    matchedDll
                                );
                        }
                        else
                            return kv.ToStoredFile(
                                    new GamePath(Constants.GameMainUE, Constants.InjectorModsPath.Join(kv.FileName()))
                                );
                    }
                default:
                    {
                        var matchesGameFles = gameFilesLookup[kv.FileName()];
                        if (matchesGameFles.Any()) // if File exists in game dir replace it
                        {
                            var matchedFile = matchesGameFles.First();
                            return kv.ToStoredFile(
                                    matchedFile
                                );
                        }
                        else
                        {
                            Logger.LogWarning("File {} is of unrecognized type {}, skipping", kv.Path().FileName, kv.Path().Extension);
                            return null;
                        }
                    }
            }
        }).OfType<TempEntity>().ToArray();

        if (modFiles.Length == 0)
        {
            Logger.LogError("0 files were processed");
            return [];
        }
        else if (modFiles.Length != achiveFiles.Length)
        {
            Logger.LogWarning("Of {} files in archive only {} were processed", achiveFiles.Length, modFiles.Length);
        }

        var Name = info.ModName;
        var Id = info.BaseModId;
        string? Version = null;

        // If ModName ends with archive extension try to parse name and version out of the archive name
        if (info.ModName != null && Constants.ArchiveExts.Contains(info.ModName.ToRelativePath().Extension))
        {
            foreach (var regex in ModArchiveNameRegexes)
            {
                var match = regex.Match(info.ModName);
                if (match.Success)
                {
                    if (match.Groups.ContainsKey("modName"))
                    {
                        Name = match.Groups["modName"].Value
                            .Replace('_', ' ')
                            .Trim();
                    }

                    if (match.Groups.ContainsKey("version"))
                    {
                        Version = match.Groups["version"].Value
                            .Replace('-', '.');
                    }

                    break;
                }
            }
        }

        return [ new ModInstallerResult
        {
            Id = Id,
            Files = modFiles,
            Name = Name,
            Version = Version,
        }];
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var achiveFiles = libraryArchive.GetTree().EnumerateChildrenBfs().ToArray();

        if (achiveFiles.Length == 0)
        {
            Logger.LogError("Archive contains 0 files");
            return new NotSupported();
        }

        var foundGameFilesGroup = LoadoutGameFilesGroup
            .FindByGameMetadata(loadout.Db, loadout.Installation.GameInstallMetadataId)
            .TryGetFirst(x => x.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId, out var gameFilesGroup);

        if (!foundGameFilesGroup)
        {
            Logger.LogError("Unable to find game files group!");
            return new NotSupported();
        }

        var gameFilesLookup = gameFilesGroup.AsLoadoutItemGroup().Children
            .Select(gameFile => gameFile.TryGetAsLoadoutItemWithTargetPath(out var targeted) ? (GamePath)targeted.TargetPath : default)
            .Where(x => x != default)
            .ToLookup(x => x.FileName);

        var modFiles = achiveFiles.Select(kv =>
        {
            var filePath = kv.Value.Item.Path;

            var matchesGameFles = gameFilesLookup[filePath.FileName];
            if (matchesGameFles.Any()) // if Content file exists in game dir replace it
            {
                var matchedFile = matchesGameFles.First();
                Logger.LogDebug("Found existing file {}, replacing", matchedFile);
                return kv.Value.ToLoadoutFile(loadout.Id, loadoutGroup.Id, transaction, matchedFile);
            }

            switch (filePath.Extension)
            {
                case Extension ext when Constants.ContentExts.Contains(ext):
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, new GamePath(Constants.GameMainUE, Constants.ContentModsPath.Join(filePath.FileName))
                                    );
                    }
                case Extension ext when ext == Constants.DLLExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, new GamePath(Constants.GameMainUE, Constants.InjectorModsPath.Join(filePath.FileName))
                                    );

                    }
                case Extension ext when ext == Constants.SavedGameExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, new GamePath(LocationId.Saves, filePath.FileName)
                                    );
                    }
                case Extension ext when ext == Constants.ConfigExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, new GamePath(LocationId.AppData, Constants.ConfigPath.Join(filePath.FileName))
                                    );
                    }
                default:
                    {
                        Logger.LogWarning("File {} is of unrecognized type {}, skipping", filePath.FileName, filePath.Extension);
                        return null;
                    }
            }
        }).OfType<LoadoutFile.New>().ToArray();

        if (modFiles.Length == 0)
        {
            Logger.LogError("0 files were processed");
            return new NotSupported();
        }
        else if (modFiles.Length != achiveFiles.Length)
        {
            Logger.LogWarning("Of {} files in archive only {} were processed", achiveFiles.Length, modFiles.Length);
        }

        return new Success();
    }
}
