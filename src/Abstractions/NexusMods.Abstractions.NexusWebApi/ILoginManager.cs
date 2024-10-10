using NexusMods.Abstractions.NexusWebApi.Types;
using R3;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Gives you information tied to the current user's login status.
/// </summary>
public interface ILoginManager
{
    /// <summary>
    /// Observable about user info.
    /// </summary>
    Observable<UserInfo?> UserInfoObservable { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    IObservable<bool> IsLoggedInObservable => UserInfoObservable.Select(static x => x is not null).DistinctUntilChanged().AsSystemObservable();

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    IObservable<bool> IsPremiumObservable => UserInfoObservable.WhereNotNull().Select(static x => x.IsPremium).DistinctUntilChanged().AsSystemObservable();

    /// <summary>
    /// The user's avatar
    /// </summary>
    IObservable<Uri?> AvatarObservable => UserInfoObservable.Select(static x => x?.AvatarUrl).DistinctUntilChanged().AsSystemObservable();

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
