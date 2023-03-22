namespace NexusMods.DataModel.Interprocess.Jobs;

public enum JobType
{
    /// <summary>
    /// The game's files are being indexed the loadout is being created.
    /// </summary>
    ManageGame,

    /// <summary>
    /// The app is currently trying to log into the Nexus
    /// </summary>
    NexusLogin,
}
