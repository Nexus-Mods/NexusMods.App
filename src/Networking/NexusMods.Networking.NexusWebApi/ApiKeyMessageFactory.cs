using System.Text;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Injects Nexus API keys into HTTP messages.
/// </summary>
public class ApiKeyMessageFactory : IHttpMessageFactory
{
    private static readonly Id ApiKeyId = new IdVariableLength(EntityCategory.AuthData, "NexusMods.Networking.NexusWebApi.ApiKey"u8.ToArray());

    private readonly IDataStore _store;
    
    private string? EnvironmentApiKey => Environment.GetEnvironmentVariable("NEXUS_API_KEY");

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
}