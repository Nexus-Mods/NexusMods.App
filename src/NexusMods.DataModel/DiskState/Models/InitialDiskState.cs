using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.DiskState.Models;

/// <summary>
///     A sibling of <see cref="DiskStateModels"/>, but only for the initial state.
/// </summary>
/// <remarks>
///     We don't want to keep history in <see cref="DiskStateModels"/> as that is only
///     supposed to hold the latest state, so in order to keep things clean,
///     we separated this out to the class.
///
///     This will also make cleaning out loadouts in MneumonicDB easier in the future.
/// </remarks>
[Include<DataModel.DiskState.Models.DiskState>]
public partial class InitialDiskState : IModelDefinition
{
    private static readonly string Namespace = "NexusMods.DataModel.InitialDiskStates";

    public static readonly ReferenceAttribute<GameMetadata> Game = new(Namespace, nameof(Game)) { IsIndexed = true, NoHistory = true };
}
