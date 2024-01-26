using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// A message factory with the express purpose of applying authentication information
/// to http requests. This is to abstract away API differences in how we acquire
/// user information between different authentication methods
/// </summary>
public interface IAuthenticatingMessageFactory : IHttpMessageFactory
{
    /// <summary>
    /// Verify that the authentication information we have for a user is valid
    /// </summary>
    /// <param name="nexusApiNexusApiClient">api nexusApiClient to use for making api requests</param>
    /// <param name="token">cancellation token</param>
    /// <returns>information about the user, null if not valid</returns>
    public ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken token);
}
