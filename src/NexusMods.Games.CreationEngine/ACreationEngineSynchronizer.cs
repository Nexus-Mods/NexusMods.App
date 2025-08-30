using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine;

public abstract class ACreationEngineSynchronizer : ALoadoutSynchronizer
{
    private static readonly Extension BSA = new(".bsa"); 
    private static readonly Extension BA2 = new(".ba2");
    
    protected ACreationEngineSynchronizer(IServiceProvider provider) : base(provider)
    {
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        // Don't backup BSA files by default
        return path.Extension == BSA || path.Extension == BA2;
    }
}
