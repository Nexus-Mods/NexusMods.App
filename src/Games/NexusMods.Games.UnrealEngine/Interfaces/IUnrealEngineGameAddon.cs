using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.IO;

namespace NexusMods.Games.UnrealEngine.Interfaces;

public interface IUnrealEngineGameAddon
{
    public string GameFolderName { get; }
    public NamedLink UE4SSLink { get; }
    public IEnumerable<FAesKey>? AESKeys { get; }
    public VersionContainer? VersionContainer { get; }
    public IStreamFactory? GetMemberVariableTemplate => null;
}
