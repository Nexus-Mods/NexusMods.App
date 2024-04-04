using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

using SMAPIToGameMapping = ImmutableDictionary<string, SMAPIGameVersionDiagnosticEmitter.GameVersions>;
using GameToSMAPIMapping = ImmutableDictionary<string, string[]>;

[UsedImplicitly]
public class SMAPIGameVersionDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    private static readonly NamedLink NexusModsSMAPILink = new("Nexus Mods", new Uri("https://nexusmods.com/stardewvalley/mods/2400"));

    public SMAPIGameVersionDiagnosticEmitter(
        ILogger<SMAPIGameVersionDiagnosticEmitter> logger,
        HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var smapiToGameMappings = await FetchSMAPIToGameMappings(cancellationToken);
        if (smapiToGameMappings is null) yield break;

        var gameToSMAPIMappings = await FetchGameToSMAPIMappings(cancellationToken);
        if (gameToSMAPIMappings is null) yield break;

        var gameVersion = loadout.Installation.Version;
        var smapiMod = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .Select(kv => kv.Value)
            .FirstOrDefault(mod => mod.Metadata.OfType<SMAPIMarker>().Any());

        if (smapiMod is null) yield break;
        var smapiMarker = smapiMod.Metadata.OfType<SMAPIMarker>().First();
        var smapiVersion = smapiMarker.Version!;

        var diagnostic1 = GameVersionOlderThanMinimumGameVersion(
            smapiToGameMappings, gameToSMAPIMappings,
            loadout, smapiMod,
            gameVersion, smapiVersion
        );

        if (diagnostic1 is not null) yield return diagnostic1;
    }

    private Diagnostic? GameVersionOlderThanMinimumGameVersion(
        SMAPIToGameMapping smapiToGameMappings,
        GameToSMAPIMapping gameToSMAPIMappings,
        Loadout loadout,
        Mod smapiMod,
        Version gameVersion,
        Version smapiVersion)
    {
        if (!smapiToGameMappings.TryGetValue(smapiVersion.ToString(), out var supportedGameVersions))
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogWarning("Found no game version information for SMAPI version {SMAPIVersion}", smapiVersion);
            return null;
        }

        var (sMinimumGameVersion, _) = supportedGameVersions;
        if (sMinimumGameVersion is null) return null;

        if (!Version.TryParse(sMinimumGameVersion, out var minimumGameVersion))
        {
            _logger.LogWarning("Unable to parse game version string `{VersionString}` as a Version", sMinimumGameVersion);
            return null;
        }

        if (minimumGameVersion <= gameVersion) return null;

        if (!gameToSMAPIMappings.TryGetValue(gameVersion.ToString(), out var supportedSMAPIVersions) ||
            supportedSMAPIVersions.Length == 0)
        {
            _logger.LogWarning("Found no supported SMAPI versions for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateGameVersionOlderThanMinimumGameVersion(
            SMAPIMod: smapiMod.ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MinimumGameVersion: sMinimumGameVersion,
            CurrentGameVersion: gameVersion.ToString(),
            LastSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersions.First(),
            SMAPINexusModsLink: NexusModsSMAPILink
        );
    }

    private static readonly Uri SMAPIToGameMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/smapi-game-versions.json");
    private static readonly Uri GameToSMAPIMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/game-smapi-versions.json");

    private SMAPIToGameMapping? _smapiToGameMappings;
    private GameToSMAPIMapping? _gameToSMAPIMappings;

    private async Task<SMAPIToGameMapping?> FetchSMAPIToGameMappings(CancellationToken cancellationToken)
    {
        return _smapiToGameMappings ??= await FetchData<SMAPIToGameMapping>(GameToSMAPIMappingsDataUri, cancellationToken);
    }

    private async Task<GameToSMAPIMapping?> FetchGameToSMAPIMappings(CancellationToken cancellationToken)
    {
        return _gameToSMAPIMappings ??= await FetchData<GameToSMAPIMapping>(SMAPIToGameMappingsDataUri, cancellationToken);
    }

    private async Task<T?> FetchData<T>(Uri dataUri, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var data = await _client.GetFromJsonAsync<T>(dataUri, cancellationToken).ConfigureAwait(false);
            if (data is not null) return data;

            _logger.LogWarning("Serialization of JSON data at {Uri} failed and returned null", dataUri);
            return null;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while getting JSON data from {Uri}", dataUri);
            return null;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record GameVersions
    {
        public required string? MinimumGameVersion { get; init; }

        public required string? MaximumGameVersion { get; init; }

        public void Deconstruct(out string? minimumGameVersion, out string? maximumGameVersion)
        {
            minimumGameVersion = MinimumGameVersion;
            maximumGameVersion = MaximumGameVersion;
        }
    }
}
