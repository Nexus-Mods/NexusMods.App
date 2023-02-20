using System.Collections.Immutable;
using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.MountAndBladeBannerlord.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.MountAndBladeBannerlord;

internal sealed class MountAndBladeBannerlordModInstaller : IModInstaller
{
    public MountAndBladeBannerlordModInstaller(LauncherManagerFactory launcherManagerFactory, IDataStore store)
    {
        _launcherManagerFactory = launcherManagerFactory;
        _store = store;
    }
    
    private static readonly RelativePath ModFolder = Constants.ModulesFolder.ToRelativePath();
    private static readonly RelativePath SubModuleFile = Constants.SubModuleName.ToRelativePath();
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly IDataStore _store;

    // TODO: We had in mind creating optional mod types (Framework, Gameplay, Assets, etc) that we potentially could map to priorities
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<MountAndBladeBannerlord>()) return Common.Priority.None;

        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.TestModuleContent(files.Select(x => x.Key.ToString()).ToArray());
        
        return result.Supported 
            ? Common.Priority.Normal 
            : Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<MountAndBladeBannerlord>()) return Enumerable.Empty<AModFile>();
        
        var modFolder = files.Keys.First(m => m.FileName == SubModuleFile).Parent;
        
        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.InstallModuleContent(files.Select(x => x.Key.ToString()).ToArray(), modFolder.ToString());
        var moduleInfos = result.Instructions.OfType<ModuleInfoInstallInstruction>().Select(x => x.ModuleInfo);
        return result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
        {
            var relativePath = instruction.Source.ToRelativePath();
            var file = files[relativePath];
            return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, ModFolder.Join(relativePath)),
                From = new HashRelativePath(srcArchive, relativePath),
                Hash = file.Hash,
                Size = file.Size,
                Store = _store,
                Metadata = new IModFileMetadata[]
                {
                    new ModuleIdMetadata
                    {
                        ModuleId = instruction.ModuleId
                    }
                }.ToImmutableHashSet()
            };
        });
    }
}