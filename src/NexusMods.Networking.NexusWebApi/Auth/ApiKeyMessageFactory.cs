using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Injects Nexus API keys into HTTP messages.
/// </summary>
public class ApiKeyMessageFactory(IConnection conn) : BaseHttpMessageFactory, IAuthenticatingMessageFactory
{
    /// <summary>
    /// The name of the environment variable that contains the API key.
    /// </summary>
    public const string NexusApiKeyEnvironmentVariable = "NEXUS_API_KEY";
    
    private string? EnvironmentApiKey => Environment.GetEnvironmentVariable(NexusApiKeyEnvironmentVariable);

    private string ApiKey
    {
        get
        {
            var value = NexusMods.Networking.NexusWebApi.Auth.ApiKey.Get(conn.Db);
            if (!string.IsNullOrWhiteSpace(value)) return value;

            return EnvironmentApiKey ?? throw new Exception("No API key set");
        }
    }

    /// <inheritdoc />
    public override async ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var requestMessage = await base.Create(method, uri);
        requestMessage.Headers.Add("apikey", ApiKey);
        return requestMessage;
    }

    /// <inheritdoc />
    public override ValueTask<bool> IsAuthenticated()
    {
        var dataStoreResult = !string.IsNullOrWhiteSpace(Auth.ApiKey.Get(conn.Db));
        return ValueTask.FromResult(dataStoreResult || EnvironmentApiKey != null);
    }

    /// <summary>
    /// Sets the API key used for future requests.
    /// </summary>
    /// <param name="apiKey">The new API key set.</param>
    public async ValueTask SetApiKey(string apiKey)
    {
        await Auth.ApiKey.Set(conn, apiKey);
    }

    /// <inheritdoc/>
    public async ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken token)
    {
        var result = await nexusApiNexusApiClient.Validate(token);
        return new UserInfo
        {
            UserId = result.Data.UserId,
            Name = result.Data.Name,
            UserRole = result.Data.IsPremium ? UserRole.Premium : result.Data.IsSupporter ? UserRole.Supporter : UserRole.Free,
            AvatarUrl = result.Data.ProfileUrl,
        };
    }
}
