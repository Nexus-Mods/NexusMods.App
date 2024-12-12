using System.Net.Http.Json;
using System.Web;
using DynamicData.Kernel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.GOG.DTOs;
using NexusMods.Networking.GOG.Models;

namespace NexusMods.Networking.GOG;

public class Client
{
    private readonly IOAuthUserInterventionHandler _oAuthUserInterventionHandler;
    private readonly ILogger<Client> _logger;
    private readonly IConnection _connection;
    private readonly HttpClient _client;

    private static Uri _authorizationUri =
        new("https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri=https://embed.gog.com/on_login_success?origin=client&response_type=code&layout=galaxy");

    private static Uri _callbackPrefix = new("https://embed.gog.com/on_login_success");
    
    private static Uri _tokenUri = new("https://auth.gog.com/token");
    

    public Client(ILogger<Client> logger, IConnection connection, IOAuthUserInterventionHandler oAuthUserInterventionHandler, HttpClient client)
    {
        _client = client;
        _logger = logger;
        _connection = connection;
        _oAuthUserInterventionHandler = oAuthUserInterventionHandler;
    }


    public async Task Login(CancellationToken token)
    {
        var request = await _oAuthUserInterventionHandler.HandleOAuthRequest(new OAuthLoginRequest
            {
                AuthorizationUrl = _authorizationUri,
                CallbackType = CallbackType.Capture,
                CallbackPrefix = _callbackPrefix,
            }
        , token);
        if (request == null)
        {
            _logger.LogWarning("The OAuth login request was cancelled.");
            return;
        }
        
        var parsed = HttpUtility.ParseQueryString(request.Query);
        var code = parsed["code"];
        if (code == null)
        {
            _logger.LogError("The OAuth login request did not contain a code.");
            return;
        }

        var tokenQuery = new Dictionary<string, string?>()
        {
            { "client_id", "46899977096215655" },
            { "client_secret", "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9" },
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", "https://embed.gog.com/on_login_success?origin=client" },
        };
        
        var uri = new Uri(QueryHelpers.AddQueryString(_tokenUri.ToString(), tokenQuery));
        
        var tokenResponse = await _client.GetFromJsonAsync<TokenResponse>(uri, token);
        
        if (tokenResponse == null)
        {
            _logger.LogError("The OAuth login request did not return a token.");
            return;
        }
        
        using var tx = _connection.BeginTransaction();
        var e = tx.TempId();
        if (TryGetAuthInfo(out var found))
            e = found.Id;
        
        tx.Add(e, AuthInfo.AccessToken, tokenResponse.AccessToken);
        tx.Add(e, AuthInfo.RefreshToken, tokenResponse.RefreshToken);
        tx.Add(e, AuthInfo.ExpriesAt, DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
        tx.Add(e, AuthInfo.SessionId, tokenResponse.SessionId);
        tx.Add(e, AuthInfo.UserId, ulong.Parse(tokenResponse.UserId));
        await tx.Commit();
        _logger.LogInformation("Logged in successfully to GOG.");
    }
    
    /// <summary>
    /// Try to get the authentication information from the database.
    /// </summary>
    public bool TryGetAuthInfo(out AuthInfo.ReadOnly authInfo)
    {
        foreach (var found in AuthInfo.All(_connection.Db))
        {
            authInfo = found;
            return true;
        }
        authInfo = default(AuthInfo.ReadOnly);
        return false;
    }

    /// <summary>
    /// Try to log out the user.
    /// </summary>
    public async Task LogOut()
    {
        using var tx = _connection.BeginTransaction();
        foreach (var found in AuthInfo.All(_connection.Db))
        {
            tx.Delete(found, false);
        }
        await tx.Commit();
    }
    
}
