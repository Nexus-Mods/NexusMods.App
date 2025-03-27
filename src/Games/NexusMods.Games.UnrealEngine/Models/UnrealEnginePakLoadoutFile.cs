using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.UnrealEngine.Models;

[Include<LoadoutFile>]
public partial class UnrealEnginePakLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.UnrealEngine.UnrealEnginePakLoadoutFile";

    /// <summary>
    /// Marker for pak file (if there is one).
    /// </summary>
    public static readonly MarkerAttribute PakFile = new(Namespace, nameof(PakFile));
}
