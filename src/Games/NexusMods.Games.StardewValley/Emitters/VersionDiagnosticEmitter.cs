using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.Paths;
using StardewModdingAPI.Toolkit;

namespace NexusMods.Games.StardewValley.Emitters;

[UsedImplicitly]
public class VersionDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IFileStore _fileStore;
    private readonly IOSInformation _os;
    private readonly ISMAPIWebApi _smapiWebApi;

    public VersionDiagnosticEmitter(
        ILogger<VersionDiagnosticEmitter> logger,
        IFileStore fileStore,
        IOSInformation os,
        ISMAPIWebApi smapiWebApi)
    {
        _logger = logger;
        _fileStore = fileStore;
        _os = os;
        _smapiWebApi = smapiWebApi;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var gameVersion = new SemanticVersion(loadout.Installation.Version);
        // var gameVersion = new SemanticVersion("1.5.6");

        var optionalSMAPIMod = loadout.GetFirstModWithMetadata<SMAPIMarker>();
        if (!optionalSMAPIMod.HasValue) yield break;

        var (_, smapiMarker) = optionalSMAPIMod.Value;
        if (!smapiMarker.TryParse(out var smapiVersion)) yield break;

        var smapiMods = await Helpers
            .GetAllManifestsAsync(_logger, _fileStore, loadout, onlyEnabledMods: true, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var modPageUrls = await _smapiWebApi.GetModPageUrls(
            _os,
            gameVersion,
            smapiVersion,
            smapiMods.Select(tuple => tuple.Item2.UniqueID).ToArray()
        );

        foreach (var tuple in smapiMods)
        {
            var (mod, manifest) = tuple;

            var minimumApiVersion = manifest.MinimumApiVersion;
            var minimumGameVersion = manifest.MinimumGameVersion;

            if (minimumApiVersion is not null && smapiVersion.IsOlderThan(minimumApiVersion))
            {
                yield return Diagnostics.CreateSMAPIVersionOlderThanMinimumAPIVersion(
                    Mod: mod.ToReference(loadout),
                    ModName: mod.Name,
                    MinimumAPIVersion: minimumApiVersion.ToString(),
                    CurrentSMAPIVersion: smapiVersion.ToString(),
                    NexusModsLink: modPageUrls.GetValueOrDefault(manifest.UniqueID, Helpers.NexusModsLink),
                    SMAPINexusModsLink: Helpers.SMAPILink
                );
            }

            if (minimumGameVersion is not null && gameVersion.IsOlderThan(minimumGameVersion))
            {
                yield return Diagnostics.CreateGameVersionOlderThanModMinimumGameVersion(
                    Mod: mod.ToReference(loadout),
                    ModName: mod.Name,
                    MinimumGameVersion: minimumGameVersion.ToString(),
                    CurrentGameVersion: gameVersion.ToString(),
                    NexusModsLink: modPageUrls.GetValueOrDefault(manifest.UniqueID, Helpers.NexusModsLink)
                );
            }
        }
    }
}
