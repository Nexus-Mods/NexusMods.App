namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Information about a logged in user
/// </summary>
public record UserInfo
{
    /// <summary>
    /// user name
    /// </summary>
    public string Name = "";

    /// <summary>
    /// is the user premium?
    /// </summary>
    public bool IsPremium;

    /// <summary>
    /// is the user a supporter (e.g. formerly premium)?
    /// </summary>
    public bool IsSupporter;
}
