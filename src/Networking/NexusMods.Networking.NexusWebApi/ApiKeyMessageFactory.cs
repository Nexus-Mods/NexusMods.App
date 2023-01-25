using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.Networking.NexusWebApi;

public class ApiKeyMessageFactory : IHttpMessageFactory
{
    private static Id ApiKeyId = new IdVariableLength(EntityCategory.AuthData, "NexusMods.Networking.NexusWebApi.ApiKey"u8.ToArray()); 
    
    private readonly ILogger<ApiKeyMessageFactory> _logger;
    private readonly IDataStore _store;

    public ApiKeyMessageFactory(ILogger<ApiKeyMessageFactory> logger, IDataStore store)
    {
        _logger = logger;
        _store = store;
    }
    
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var msg = new HttpRequestMessage(method, uri);
        msg.Headers.Add("apikey", ApiKey);
        return ValueTask.FromResult(msg);
    }
    
    private string ApiKey
    {
        get
        {
            var value = Encoding.UTF8.GetString(_store.GetRaw(ApiKeyId) ?? Array.Empty<byte>());
            if (!string.IsNullOrWhiteSpace(value)) return value;
               return EnvironmentApiKey ?? throw new Exception("No API key set");
        }
    }

    public async ValueTask<bool> IsAuthenticated()
    { 
        var dataStoreResult = await ValueTask.FromResult(_store.GetRaw(ApiKeyId) != null);
        return dataStoreResult || EnvironmentApiKey != null;
    }

    private string? EnvironmentApiKey => Environment.GetEnvironmentVariable("NEXUS_API_KEY");
    
    public ValueTask SetApiKey(string apiKey)
    {
        _store.PutRaw(ApiKeyId, Encoding.UTF8.GetBytes(apiKey));
        return ValueTask.CompletedTask;
    }
}