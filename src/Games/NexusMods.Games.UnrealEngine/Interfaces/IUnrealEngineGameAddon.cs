using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine.Interfaces;

public interface IUnrealEngineGameAddon
{
    /// <summary>
    /// The name of the folder which includes the content and binaries folders.
    /// </summary>
    public RelativePath RelPathGameName { get; }
    
    /// <summary>
    /// Link to the Scripting System download page which is used in diagonstics if
    ///  we detect mods that require it.
    /// </summary>
    public NamedLink UE4SSLink { get; }
    
    /// <summary>
    /// The expected destination of pak mods. This is usually "Content/Paks/~mods" but
    ///  some games may have a different location. (Hogwarts)
    /// </summary>
    public RelativePath? RelPathPakMods { get; }
    
    /// <summary>
    /// The AES keys used to decrypt and extract pak files. At time of writing, the
    ///  extraction functionality has yet to be written - we might want to keep it
    ///  that way for now.
    /// </summary>
    public IEnumerable<FAesKey>? AESKeys { get; }
    
    /// <summary>
    /// The version of the Unreal Engine Game. Not providing this will default to UE4.
    /// </summary>
    public VersionContainer? VersionContainer { get; }

    /// <summary>
    /// Stream in the member variable layout template as expected by the scripting system
    ///  so that mods can enumerate and access the member variables.
    /// This is usually not required, but some games may have custom offsets.
    /// </summary>
    public IStreamFactory? MemberVariableTemplate { get; }
    
    /// <summary>
    /// The architecture file path segment to use when creating paths.
    ///  This is usually "WinGDK" for xbox game pass, or "Win64" for other stores.
    /// I may be wrong, but IIRC, the architecture segment can at times include the
    ///  store name in some situations (GOGWin64? or something similar).
    /// </summary>
    public Func<GameLocatorResult, RelativePath>? ArchitectureSegmentRetriever { get; }
}
