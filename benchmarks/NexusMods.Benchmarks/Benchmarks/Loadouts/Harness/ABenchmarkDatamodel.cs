using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
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
    public new IGame Game => base.Game;
    public new GameInstallation Install => base.Install;
    public new Loadout.ReadOnly BaseLoadout => base.BaseLoadout;
    
    public static ABenchmarkDatamodel WithMod(IServiceProvider provider, string modName, IEnumerable<string> files)
    {
        throw new NotImplementedException();
        /*
        var setup = new ABenchmarkDatamodel(provider);
        var modFiles = files.Select(x => (x, x)).ToArray();
        Task.Run(async () =>
            {
                await setup.InitializeAsync();
                return await setup.AddMod(modName, modFiles);
            }
        ).Wait();
        return setup;
        */
    }
}
