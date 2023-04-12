namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Status of a given Mod
/// </summary>
public enum ModStatus
{
    /// <summary>
    /// The mod is installed.
    /// </summary>
    Installed,
    /// <summary>
    /// The mod is currently being installed.
    /// </summary>
    Installing,
    /// <summary>
    /// The mod install has failed.
    /// </summary>
    Failed
}
