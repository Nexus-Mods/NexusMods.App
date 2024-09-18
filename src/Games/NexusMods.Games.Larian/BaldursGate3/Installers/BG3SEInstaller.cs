using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.Larian.BaldursGate3.Installers;

public class BG3SEInstaller : ALibraryArchiveInstaller
{
    public BG3SEInstaller(IServiceProvider serviceProvider) : 
        base(serviceProvider, serviceProvider.GetRequiredService<ILogger<BG3SEInstaller>>())
    {
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();
        var nodes = tree.FindSubPathsByKeyUpward(["DWrite.dll"]);
        if (nodes.Count == 0)
            return ValueTask.FromResult<InstallerResult>(new NotSupported());
        var dllNode = nodes[0];
        var parent = dllNode.Parent() ?? tree;

        return ValueTask.FromResult<InstallerResult>(new NotSupported());
    }
}
