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
    /// Returns true if the user is logged in.
    /// There could be a delay between the user logging in and this value being updated.
    /// Prefer <see cref="GetIsUserLoggedInAsync"/> to get the most up-to-date information.
    /// </summary>
    bool IsLoggedIn => UserInfo is not null;
    
    /// <summary>
    /// Returns true if the user is logged in and is a premium member
    /// </summary>
    bool IsPremium => UserInfo?.UserRole == UserRole.Premium;
    
    /// <summary>
    /// Returns the users login information
    /// </summary>
    UserInfo? UserInfo { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    IObservable<bool> IsLoggedInObservable => UserInfoObservable.Select(static x => x is not null).DistinctUntilChanged().AsSystemObservable();

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    IObservable<bool> IsPremiumObservable => UserInfoObservable.WhereNotNull().Select(static x => x.UserRole == UserRole.Premium).DistinctUntilChanged().AsSystemObservable();
    
    /// <summary>
    /// The user's role
    /// </summary>
    IObservable<UserRole> UserRoleObservable => UserInfoObservable.WhereNotNull().Select(static x => x.UserRole).DistinctUntilChanged().AsSystemObservable();
    
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
    /// Verifies whether the user is logged in or not
    /// </summary>
    Task<bool> GetIsUserLoggedInAsync(CancellationToken token = default);

    /// <summary>
    /// Ensures that the user is logged in by showing a message if they are not prompting them to do so
    /// </summary>
    /// <param name="message">Message describing the operation that requires the user to be logged in</param>
    /// <Returns>Returns false if user is not logged or cancels the log in operation</Returns>
    public Task<bool> EnsureLoggedIn(string message, CancellationToken token = default);

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    Task Logout();
}
