using JetBrains.Annotations;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Game support type.
/// </summary>
[PublicAPI]
public enum SupportType
{
    /// <summary>
    /// The game is unsupported.
    /// </summary>
    Unsupported = 0,

    /// <summary>
    /// The game is officially supported and the extension maintained by Nexus Mods.
    /// </summary>
    Official = 1,

    /// <summary>
    /// The game is supported and the extension is maintained by the community.
    /// </summary>
    Community = 2,
}
