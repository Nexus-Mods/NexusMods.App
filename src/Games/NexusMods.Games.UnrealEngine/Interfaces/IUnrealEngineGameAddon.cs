using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine.Interfaces;

public interface IUnrealEngineGameAddon
{
    public RelativePath RelPathGameName { get; }
    public NamedLink UE4SSLink { get; }
    
    public RelativePath? RelPathPakMods { get; }
    public IEnumerable<FAesKey>? AESKeys { get; }
    public VersionContainer? VersionContainer { get; }
    public IStreamFactory? MemberVariableTemplate { get; }
    public Func<GameLocatorResult, RelativePath>? ArchitectureSegmentRetriever { get; }
}
