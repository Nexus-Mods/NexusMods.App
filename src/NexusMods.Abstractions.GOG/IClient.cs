using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GOG;

/// <summary>
/// Interface for the GOG API client.
/// </summary>
public interface IClient
{
    /// <summary>
    /// Notify the client of a new auth url being passed in from the CLI.
    /// </summary>
    public void AuthUrl(NXMGogAuthUrl url);

    /// <summary>
    /// Initiate the login process.
    /// </summary>
    public Task Login(CancellationToken token);
    
    /// <summary>
    /// Get all the builds for a given product and OS.
    /// </summary>
    public Task<Build[]> GetBuilds(ProductId productId, OS os, CancellationToken token);

    /// <summary>
    /// Get the depot information for a build.
    /// </summary>
    public Task<DepotInfo> GetDepot(Build build, CancellationToken token);

    /// <summary>
    /// Given a depot, a build, and a path, return a stream to the file. This file is seekable, and will cache and
    /// stream in data as required from the CDN.
    /// </summary>
    public Task<Stream> GetFileStream(Build build, DepotInfo depotInfo, RelativePath path, CancellationToken token);
}
