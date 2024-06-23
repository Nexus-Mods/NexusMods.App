using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Gives you information tied to the current user's login status.
/// </summary>
public interface ILoginManager
{
    
    /// <summary>
    /// Allows you to subscribe to notifications of when the user information changes.
    /// </summary>
    IObservable<UserInfo?> UserInfoObservable { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    IObservable<bool> IsLoggedInObservable { get; }

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    IObservable<bool> IsPremiumObservable { get; }

    /// <summary>
    /// The user's avatar
    /// </summary>
    IObservable<Uri?> AvatarObservable { get; }

    /// <summary>
    /// Show a browser and log into Nexus Mods
    /// </summary>
    /// <param name="token"></param>
    Task LoginAsync(CancellationToken token = default);
    
    /// <summary>
    /// Returns the user's information
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync(CancellationToken token = default);

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    Task Logout();
}
