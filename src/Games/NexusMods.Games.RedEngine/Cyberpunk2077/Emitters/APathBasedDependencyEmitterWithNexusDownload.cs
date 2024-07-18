using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// A path based dependency emitter with a nexus download link, for example Cyber Engine Tweaks is mod id 107 for Cyberpunk 2077.
/// </summary>
public abstract class APathBasedDependencyEmitterWithNexusDownload : APathBasedDependencyEmitter
{
    protected override NamedLink DownloadLink => new("Nexus Mods", new($"https://www.nexusmods.com/{Domain.Value}/mods/{ModId}"));
 
    /// <summary>
    /// The domain of the game this emitter is for.
    /// </summary>
    protected abstract GameDomain Domain { get; }
    
    /// <summary>
    /// The nexus Mod ID of the of the dependency.
    /// </summary>
    protected abstract ModId ModId { get; }
}
