using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.CreationEngine.Installers;

/// <summary>
/// Handles the odd cases of archives with random files in random positions, but that are generally just BSA/Plugins. This installer will just dump the supported files
/// into {Game}/Data and ignore ignored files
/// </summary>
public class FallbackInstaller : ALibraryArchiveInstaller
{
    private static readonly Extension[] SupportedExtensions = [KnownCEExtensions.BSA, KnownCEExtensions.BA2, KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL];
    private static readonly Extension[] IgnoredExtensions = [KnownExtensions.Jpg, KnownExtensions.Txt];

    private static readonly RelativePath[] IgnoredFiles =
    [
        "meta.ini", // MO2 Cruft some people accidentally put in the root of the archive
        "readme.txt", // We won't let documentation stop us from installing a mod 
    ];
    
    public FallbackInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<FallbackInstaller>>())
    {
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        List<LibraryArchiveFileEntry.ReadOnly> supported = new();
        List<LibraryArchiveFileEntry.ReadOnly> ignored = new();

        foreach (var file in libraryArchive.Children)
        {
            if (SupportedExtensions.Contains(file.Path.Extension))
                supported.Add(file);
            else if (IgnoredExtensions.Contains(file.Path.Extension))
                ignored.Add(file);
            else if (IgnoredFiles.Contains(file.Path.FileName))
                ignored.Add(file);
            else
                return ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: $"Cannot handle {file.Path.Extension}"));
        }

        foreach (var file in supported)
        {
            var libFile = file.AsLibraryFile();
            _ = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, LocationId.Game, RelativePath.FromUnsanitizedInput("Data") / file.Path.FileName),
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
}
