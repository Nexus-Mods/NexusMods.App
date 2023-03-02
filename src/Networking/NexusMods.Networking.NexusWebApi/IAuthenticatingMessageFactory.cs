using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// A message factory with the express purpose of applying authentication information
/// to http requests. This is to abstract away API differences in how we acquire
/// user information between different authentication methods
/// </summary>
public interface IAuthenticatingMessageFactory : IHttpMessageFactory
{
    /// <summary>
    /// verify that the authentication information we have for a user is valid
    /// </summary>
    /// <param name="client">api client to use for making api requests</param>
    /// <param name="token">cancelation token</param>
    /// <returns>information about the user, null if not valid</returns>
    public ValueTask<UserInfo?> Verify(Client client, CancellationToken token);
}
