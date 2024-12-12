namespace NexusMods.Abstractions.OAuth;

public interface IOAuthUserInterventionHandler
{
    /// <summary>
    /// Initiate the OAuth login process, using the provided information. If the login process is cancelled,
    /// this method should return null.
    /// </summary>
    public Task<Uri?> HandleOAuthRequest(OAuthLoginRequest request, CancellationToken token);
}
