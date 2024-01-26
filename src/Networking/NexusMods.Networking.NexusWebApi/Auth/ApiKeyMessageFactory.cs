using System.Text;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Injects Nexus API keys into HTTP messages.
/// </summary>
public class ApiKeyMessageFactory : IAuthenticatingMessageFactory
{
    /// <summary>
    /// The name of the environment variable that contains the API key.
    /// </summary>
    public const string NexusApiKeyEnvironmentVariable = "NEXUS_API_KEY";

    private static readonly IId ApiKeyId = new IdVariableLength(EntityCategory.AuthData, "NexusMods.Networking.NexusWebApi.ApiKey"u8.ToArray());

    private readonly IDataStore _store;

    private string? EnvironmentApiKey => Environment.GetEnvironmentVariable(NexusApiKeyEnvironmentVariable);

    private string ApiKey
    {
        get
        {
            var value = Encoding.UTF8.GetString(_store.GetRaw(ApiKeyId) ?? Array.Empty<byte>());
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return EnvironmentApiKey ?? throw new Exception("No API key set");
        }
    }

    // TODO: Remove dependency on external components here.

    /// <summary/>
    /// <param name="store"></param>
    public ApiKeyMessageFactory(IDataStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var msg = new HttpRequestMessage(method, uri);
        msg.Headers.Add("apikey", ApiKey);
        return ValueTask.FromResult(msg);
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsAuthenticated()
    {
        var dataStoreResult = await ValueTask.FromResult(_store.GetRaw(ApiKeyId) != null);
        return dataStoreResult || EnvironmentApiKey != null;
    }

    /// <summary>
    /// Sets the API key used for future requests.
    /// </summary>
    /// <param name="apiKey">The new API key set.</param>
    public ValueTask SetApiKey(string apiKey)
    {
        _store.PutRaw(ApiKeyId, Encoding.UTF8.GetBytes(apiKey));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken token)
    {
        var result = await nexusApiNexusApiClient.Validate(token);
        return new UserInfo
        {
            Name = result.Data.Name,
            IsPremium = result.Data.IsPremium,
            AvatarUrl = result.Data.ProfileUrl
        };
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken token)
    {
        return new ValueTask<HttpRequestMessage?>();
    }

    public ValueTask<UserInfo?> Verify(NexusApiClient nexusApiClient, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
