using DynamicData.Kernel;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public class SyncTreeNodeOld : ISyncNode
{
    public GamePath Path { get; init; }
    public Optional<DiskStateEntry> Disk { get; set; }
    public Optional<DiskStateEntry> Previous { get; set; }
    public Optional<StoredFile.ReadOnly> LoadoutFile { get; set; }
    public Signature Signature { get; set; }
    public Actions Actions { get; set; }
    
}
