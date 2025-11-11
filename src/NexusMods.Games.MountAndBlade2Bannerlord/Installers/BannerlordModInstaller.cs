using System.Diagnostics;
using System.Xml;
using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Models.Library;
using static NexusMods.Games.MountAndBlade2Bannerlord.BannerlordConstants;
using GameStore = Bannerlord.LauncherManager.Models.GameStore;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class BannerlordModInstaller : ALibraryArchiveInstaller
{
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly IFileStore _fileStore;

    public BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<BannerlordModInstaller>>())
    {
        _launcherManagerFactory = serviceProvider.GetRequiredService<LauncherManagerFactory>();
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var moduleInfoFileTuples = await GetModuleInfoFiles(libraryArchive, cancellationToken);
        if (moduleInfoFileTuples.Count == 0) return new NotSupported(Reason: "Found no module info files in the archive"); // TODO: Will it install in root folder the content?

        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        var store = loadout.Installation.Store;
        var isXboxStore = store == Abstractions.GameLocators.GameStore.XboxGamePass;
        var isNonXboxStore = !isXboxStore;
        
        foreach (var tuple in moduleInfoFileTuples)
        {
            var (moduleInfoFile, moduleInfo) = tuple;
            var parent = moduleInfoFile.Path.Parent;
            
            var moduleInfoWithPath = new ModuleInfoExtendedWithMetadata(moduleInfo, ModuleProviderType.Vortex, moduleInfoFile.Path);
            
            var modGroup = new LoadoutItemGroup.New(transaction, out var modGroupEntityId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(transaction, modGroupEntityId)
                {
                    Name = moduleInfo.Name,
                    LoadoutId = loadout,
                    ParentId = loadoutGroup,
                },
            };
            
            var moduleInfoLoadoutItemId = Optional<EntityId>.None;
            
            var moduleFiles = libraryArchive.Children.Where(x => x.Path.InFolder(parent)).Select(x => x.Path.ToString()).ToArray();
            // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
            var installResult = launcherManager.InstallModuleContent(moduleFiles, [moduleInfoWithPath]);
            var filesToCopy = installResult.Instructions.OfType<CopyInstallInstruction>();
            foreach (var instruction in filesToCopy)
            {
                RelativePath source = instruction.Source;
                RelativePath destination = instruction.Destination;
                moduleInfoLoadoutItemId = AddFileFromFilesToCopy(libraryArchive, transaction, loadout,
                    source, destination, modGroup, moduleInfoFile,
                    moduleInfoLoadoutItemId
                );
            }
            
            var storeFilesToCopy = installResult.Instructions.OfType<CopyStoreInstallInstruction>().ToArray();
            var hasXboxFiles = storeFilesToCopy.Any(x => x.Store == GameStore.Xbox);
            var hasNonXboxFiles = storeFilesToCopy.Any(x => x.Store != GameStore.Xbox);
            var usedDestinations = new HashSet<RelativePath>();
            
            foreach (var instruction in storeFilesToCopy)
            {
                // Note(sewer) Alias Xbox store with Steam store files, in case mod author
                //      included files for only one version of the game.
                //      For more info, see `0004-MountAndBlade2Bannerlord.md`.
                RelativePath source = instruction.Source;
                RelativePath destination = instruction.Destination;
                
                // If this mod has no files for Xbox store, and we're on Xbox store.
                // InstallModuleContent emits multiple stores for `Win64_Shipping_Client`,
                // so we want to avoid adding multiple times, hence the hashset.
                
                // Alias non-Xbox files onto Xbox files, if only non-Xbox files exist.
                if (!hasXboxFiles && isXboxStore && !usedDestinations.Contains(destination))
                {
                    destination = destination.Path.Replace("Win64_Shipping_Client", "Gaming.Desktop.x64_Shipping_Client");
                    if (!usedDestinations.Contains(destination))
                    {
                        moduleInfoLoadoutItemId = AddFileFromFilesToCopy(libraryArchive, transaction, loadout,
                            source, destination, modGroup, moduleInfoFile,
                            moduleInfoLoadoutItemId
                        );
                    }
                }

                // Alias Xbox files onto non-Xbox files, if only Xbox files exist.
                if (!hasNonXboxFiles && isNonXboxStore)
                {
                    destination = destination.Path.Replace("Gaming.Desktop.x64_Shipping_Client", "Win64_Shipping_Client");
                    if (!usedDestinations.Contains(destination))
                    {
                        moduleInfoLoadoutItemId = AddFileFromFilesToCopy(libraryArchive, transaction, loadout,
                            source, destination, modGroup, moduleInfoFile,
                            moduleInfoLoadoutItemId
                        );
                    }
                }

                if (!usedDestinations.Contains(destination))
                {
                    moduleInfoLoadoutItemId = AddFileFromFilesToCopy(libraryArchive, transaction, loadout,
                        source, destination, modGroup, moduleInfoFile,
                        moduleInfoLoadoutItemId
                    );
                }

                usedDestinations.Add(destination);
            }
            
            Debug.Assert(moduleInfoLoadoutItemId.HasValue);

            _ = new BannerlordModuleLoadoutItem.New(transaction, modGroupEntityId)
            {
                ModuleInfoId = moduleInfoLoadoutItemId.Value,
                LoadoutItemGroup = modGroup,
            };
        }

        return new Success();
    }

    private static Optional<EntityId> AddFileFromFilesToCopy(
        LibraryArchive.ReadOnly libraryArchive, ITransaction transaction, Loadout.ReadOnly loadout, RelativePath source, RelativePath destination, LoadoutItemGroup.New modGroup, LibraryArchiveFileEntry.ReadOnly moduleInfoFile, Optional<EntityId> moduleInfoLoadoutItemId)
    {
        var fileEntry = libraryArchive.Children.First(x => x.Path.Equals(source));
        var to = new GamePath(LocationId.Game, destination);
        var loadoutFile = new LoadoutFile.New(transaction, out var entityId)
        {
            Hash = fileEntry.AsLibraryFile().Hash,
            Size = fileEntry.AsLibraryFile().Size,
            LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
            {
                TargetPath = to.ToGamePathParentTuple(loadout.Id),
                LoadoutItem = new LoadoutItem.New(transaction, entityId)
                {
                    Name = fileEntry.AsLibraryFile().FileName,
                    LoadoutId = loadout,
                    ParentId = modGroup,
                },
            },
        };

        if (fileEntry.Id != moduleInfoFile.Id) 
            return moduleInfoLoadoutItemId;
 
        moduleInfoLoadoutItemId = entityId;
                
        _ = new ModuleInfoFileLoadoutFile.New(transaction, entityId)
        {
            IsModuleInfoFile = true,
            LoadoutFile = loadoutFile,
        };
        return moduleInfoLoadoutItemId;
    }

    private async ValueTask<List<ValueTuple<LibraryArchiveFileEntry.ReadOnly, ModuleInfoExtended>>> GetModuleInfoFiles(
        LibraryArchive.ReadOnly libraryArchive,
        CancellationToken cancellationToken)
    {
        var results = new List<(LibraryArchiveFileEntry.ReadOnly, ModuleInfoExtended)>();

        foreach (var fileEntry in libraryArchive.Children)
        {
            if (!fileEntry.Path.FileName.Equals(SubModuleFile)) continue;

            try
            {
                await using var stream = await _fileStore.GetFileStream(fileEntry.AsLibraryFile().Hash, token: cancellationToken);
                var doc = new XmlDocument();
                doc.Load(stream);
                var data = ModuleInfoExtended.FromXml(doc);

                results.Add((fileEntry, data));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while deserializing {Path} from {Archive}", fileEntry.Path, fileEntry.Parent.AsLibraryFile().FileName);
            }
        }

        return results;
    }
}
