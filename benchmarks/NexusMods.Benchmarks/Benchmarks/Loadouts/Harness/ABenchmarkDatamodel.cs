using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.DataModel;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Paths;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;

/// <summary>
///     Provides a datamodel library setup.
/// </summary>
public class ABenchmarkDatamodel(IServiceProvider provider) : ADataModelTest<ABenchmarkDatamodel>(provider)
{
    public new TemporaryFileManager TemporaryFileManager => base.TemporaryFileManager;
    public new IFileStore FileStore => base.FileStore;
    public new IArchiveInstaller ArchiveInstaller => base.ArchiveInstaller;

    public new LoadoutRegistry LoadoutRegistry => base.LoadoutRegistry;
    public new FileHashCache FileHashCache => base.FileHashCache;
    public new IFileSystem FileSystem => base.FileSystem;
    public new IDataStore DataStore => base.DataStore;
    public new IFileOriginRegistry FileOriginRegistry => base.FileOriginRegistry;
    public new DiskStateRegistry DiskStateRegistry => base.DiskStateRegistry;
    public new IToolManager ToolManager => base.ToolManager;

    public new IGame Game => base.Game;
    public new GameInstallation Install => base.Install;
    public new LoadoutMarker BaseList => base.BaseList;
    
    public static ABenchmarkDatamodel WithMod(IServiceProvider provider, string modName, IEnumerable<string> files)
    {
        var setup = new ABenchmarkDatamodel(provider);
        var modFiles = files.Select(x => (x, x)).ToArray();
        Task.Run(async () =>
            {
                await setup.InitializeAsync();
                return await setup.AddMod(modName, modFiles);
            }
        ).Wait();
        return setup;
    }
}
