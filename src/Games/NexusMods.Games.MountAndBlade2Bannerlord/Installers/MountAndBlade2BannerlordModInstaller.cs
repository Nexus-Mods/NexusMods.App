using System.Diagnostics;
using System.Xml;
using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths.Extensions;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class MountAndBlade2BannerlordModInstaller : ALibraryArchiveInstaller
{
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly IFileStore _fileStore;

    public MountAndBlade2BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<MountAndBlade2BannerlordModInstaller>>())
    {
        _launcherManagerFactory = serviceProvider.GetRequiredService<LauncherManagerFactory>();
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var moduleInfoFileTuples = await GetModuleInfoFiles(libraryArchive, cancellationToken);
        if (moduleInfoFileTuples.Count == 0) return new NotSupported(); // TODO: Will it install in root folder the content?

        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        
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
                var fileRelativePath = instruction.Source.ToRelativePath();
                var fileEntry = libraryArchive.Children.First(x => x.Path.Equals(fileRelativePath));

                var to = new GamePath(LocationId.Game, instruction.Destination.ToRelativePath());

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
                
                if (fileEntry.Id == moduleInfoFile.Id)
                {
                    moduleInfoLoadoutItemId = entityId;
                    _ = new ModuleInfoFileLoadoutFile.New(transaction, entityId)
                    {
                        IsModuleInfoFile = true,
                        LoadoutFile = loadoutFile,
                    };
                }
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
