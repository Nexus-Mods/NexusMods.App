using System.Collections.Concurrent;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using SteamKit2;
using SteamKit2.CDN;

namespace NexusMods.Networking.Steam;

public class CDNPool
{
    private readonly Session _session;
    
    /// <summary>
    /// CDN servers that we can use to download content.
    /// </summary>
    private ConcurrentQueue<Server> _servers = [];
    private Server? _currentServer = null;
    
    /// <summary>
    /// Cached auth tokens for CDN servers.
    /// </summary>
    private ConcurrentDictionary<(string Host, DepotId DepotId), string> _authTokens = new();

    public CDNPool(Session session)
    {
        _session = session;
    }

    internal async ValueTask<Server> GetServer()
    {
        if (_servers.IsEmpty)
        {
            var servers = await _session.Content.GetServersForSteamPipe();
            servers = servers
                .Where(c => c.Type is ("CDN" or "SteamCache"))
                .OrderBy(c => c.Type == "CDN" ? 1 : 0)
                .ThenBy(c => c.WeightedLoad)
                .ToList();
            foreach (var server in servers)
            {
                _servers.Enqueue(server);
            }
        }

        var newServer = _currentServer;
        if (_currentServer == null)
        {
            if (!_servers.TryDequeue(out newServer))
                throw new Exception("No servers available");
            _currentServer = newServer;
        }
        return newServer!;
    }
    
    internal void FailServer()
    {
        _currentServer = null;
    }
    
    

    /// <summary>
    /// Get a CDN auth token for a given depot on a given server.
    /// </summary>
    internal async Task<string> GetCDNAuthTokenAsync(AppId appId, DepotId depotId, Server server)
    {
        // Check if we already have an auth token for this server
        if (_authTokens.TryGetValue((server.Host!, depotId), out var token))
            return token;
        
        var key = await _session.Content.GetCDNAuthToken(appId.Value, depotId.Value, server.Host!);
        if (key.Result != EResult.OK)
            throw new Exception($"Failed to get CDN auth token for depot {depotId.Value}");
        
        _authTokens.TryAdd((server.Host!, depotId), key.Token);
        return key.Token;
    }

    public async Task<Manifest> GetManifestContents(AppId appId, DepotId depotId, ManifestId manifestId, string branch, CancellationToken token)
    {
        var requestCode = await _session.GetManifestRequestCodeAsync(appId, depotId, manifestId, branch);
        var depotKey = await _session.GetDepotKey(appId, depotId);
        var server = await GetServer();
        
        string? cdnAuthToken = null;
        if (server.Type == "CDN")
        {
            cdnAuthToken = await GetCDNAuthTokenAsync(appId, depotId, server);
        }

        var manifest = await _session.CDNClient.DownloadManifestAsync(depotId.Value, manifestId.Value, requestCode, server, depotKey, cdnAuthToken: cdnAuthToken);
        var parsed = ManifestParser.Parse(manifest);
        return parsed;
    }
}
