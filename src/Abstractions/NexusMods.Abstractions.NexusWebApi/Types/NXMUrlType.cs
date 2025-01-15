namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// type of url
/// </summary>
// ReSharper disable once InconsistentNaming
public enum NXMUrlType : byte
{
    /// <summary>
    /// url referencing a mod file
    /// </summary>
    Mod = 0,
    /// <summary>
    /// url referencing a collection revision
    /// </summary>
    Collection,
    /// <summary>
    /// callback for oauth login
    /// </summary>
    OAuth,
    /// <summary>
    /// allback for GOG authentication
    /// </summary>
    GogAuth,
}
