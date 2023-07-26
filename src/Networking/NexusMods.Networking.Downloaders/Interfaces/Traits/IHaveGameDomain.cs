namespace NexusMods.Networking.Downloaders.Interfaces.Traits;

/// <summary>
/// Interface implemented by <see cref="IDownloadTask"/>(s) which provide a suggestion for the related
/// <see cref="GameDomain"/>(s) known by the Nexus App.
///
/// This is for the purpose of automatically selecting suggested loadout(s) a mod should be installed into.
/// </summary>
public interface IHaveGameDomain
{
    /// <summary>
    /// Unique identifier for the game that should receive the mod.
    /// </summary>
    public string GameDomain { get; }
}
