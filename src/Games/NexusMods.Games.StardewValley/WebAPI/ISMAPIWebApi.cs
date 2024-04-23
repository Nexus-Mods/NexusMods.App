using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Paths;
using StardewModdingAPI;

namespace NexusMods.Games.StardewValley.WebAPI;

/// <summary>
/// Interface for working with the SMAPI Web API.
/// </summary>
[PublicAPI]
public interface ISMAPIWebApi : IDisposable
{
    /// <summary>
    /// Gets all mod page urls of the given mods.
    /// </summary>
    public Task<IReadOnlyDictionary<string, NamedLink>> GetModPageUrls(
        IOSInformation os,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        string[] smapiIDs
    );
}
