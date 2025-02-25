using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.Diagnostics.Values;

namespace NexusMods.Games.UnrealEngine.Interfaces;

public interface IUnrealEngineGameAddon
{
    public string GameFolderName { get; }
    public NamedLink UE4SSLink { get; }
    public FAesKey? AESKey { get; }
    public VersionContainer? VersionContainer { get; }
}
