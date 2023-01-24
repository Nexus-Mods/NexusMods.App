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
    
    private string ApiKey => Encoding.UTF8.GetString(_store.GetRaw(ApiKeyId) ?? Array.Empty<byte>());

    public ValueTask<bool> IsAuthenticated()
    {
        return ValueTask.FromResult(_store.GetRaw(ApiKeyId) != null);
    }
    
    public ValueTask SetApiKey(string apiKey)
    {
        _store.PutRaw(ApiKeyId, Encoding.UTF8.GetBytes(apiKey));
        return ValueTask.CompletedTask;
    }
}