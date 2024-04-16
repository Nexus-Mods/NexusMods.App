using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley.Emitters;

using SMAPIToGameMapping = ImmutableDictionary<Version, SMAPIGameVersionDiagnosticEmitter.GameVersions>;
using GameToSMAPIMapping = ImmutableDictionary<Version, Version>;

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

        // var gameVersion = SimplifyVersion(new Version("1.5.6.22018"));
        var gameVersion = SimplifyVersion(loadout.Installation.Version);

        var optionalSmapiMod = loadout.GetFirstModWithMetadata<SMAPIMarker>();

        if (!optionalSmapiMod.HasValue)
        {
            var smapiModCount = loadout
                .GetModsWithMetadata<SMAPIMarker>()
                .Count();

            // NOTE(erri120): The MissingSMAPIEmitter will warn the user if SMAPI is required.
            // This emitter will suggest SMAPI if there are no mods yet.
            if (smapiModCount != 0) yield break;

            var suggestion = SuggestSMAPI(gameToSMAPIMappings, gameVersion);
            if (suggestion is not null) yield return suggestion;
            yield break;
        }

        var (smapiMod, smapiMarker) = optionalSmapiMod.Value;

        // var smapiVersion = SimplifyVersion(new Version("4.0.6.1254"));
        var smapiVersion = SimplifyVersion(smapiMarker.Version!);

        if (!TryGetValue(smapiToGameMappings, smapiVersion, useEquals: true, out var supportedGameVersions))
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogWarning("Found no game version information for SMAPI version {SMAPIVersion}", smapiVersion);
            yield break;
        }

        var diagnostic1 = GameVersionOlderThanMinimumGameVersion(
            gameToSMAPIMappings,
            loadout, smapiMod,
            gameVersion, smapiVersion, supportedGameVersions
        );

        var diagnostic2 = GameVersionNewerThanMaximumGameVersion(
            gameToSMAPIMappings,
            loadout, smapiMod,
            gameVersion, smapiVersion, supportedGameVersions
        );

        if (diagnostic1 is not null) yield return diagnostic1;
        if (diagnostic2 is not null) yield return diagnostic2;
    }

    private Diagnostic? SuggestSMAPI(GameToSMAPIMapping gameToSMAPIMappings, Version gameVersion)
    {
        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: false, out var supportedSMAPIVersion))
        {
            _logger.LogWarning("Found details for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateSuggestSMAPIVersion(
            LatestSMAPIVersion: supportedSMAPIVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            SMAPINexusModsLink: NexusModsSMAPILink
        );
    }

    private Diagnostic? GameVersionNewerThanMaximumGameVersion(
        GameToSMAPIMapping gameToSMAPIMappings,
        Loadout loadout,
        Mod smapiMod,
        Version gameVersion,
        Version smapiVersion,
        GameVersions supportedGameVersions)
    {
        var (_, maximumGameVersion) = supportedGameVersions;
        if (maximumGameVersion is null) return null;

        var comparison = VersionComparer.Instance.Compare(maximumGameVersion, gameVersion);
        if (comparison >= 0) return null;

        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: false, out var supportedSMAPIVersion))
        {
            _logger.LogWarning("Found details for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateGameVersionNewerThanMaximumGameVersion(
            SMAPIMod: smapiMod.ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MaximumGameVersion: maximumGameVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            NewestSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersion.ToString(),
            SMAPINexusModsLink: NexusModsSMAPILink
        );
    }

    private Diagnostic? GameVersionOlderThanMinimumGameVersion(
        GameToSMAPIMapping gameToSMAPIMappings,
        Loadout loadout,
        Mod smapiMod,
        Version gameVersion,
        Version smapiVersion,
        GameVersions supportedGameVersions)
    {
        var (minimumGameVersion, _) = supportedGameVersions;
        if (minimumGameVersion is null) return null;

        var comparison = VersionComparer.Instance.Compare(minimumGameVersion, gameVersion);
        if (comparison <= 0) return null;

        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: true, out var supportedSMAPIVersion))
        {
            _logger.LogWarning("Found details for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateGameVersionOlderThanMinimumGameVersion(
            SMAPIMod: smapiMod.ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MinimumGameVersion: minimumGameVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            NewestSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersion.ToString(),
            SMAPINexusModsLink: NexusModsSMAPILink
        );
    }

    private static bool TryGetValue<T>(
        ImmutableDictionary<Version, T> dictionary,
        Version input,
        bool useEquals,
        [NotNullWhen(true)] out T? value) where T : class
    {
        var actualKey = dictionary
            .Keys
            .OrderDescending(VersionComparer.Instance)
            .FirstOrDefault(x => useEquals
                ? EqualityPredicate(x, input)
                : ComparisonPredicate(x, input)
            );

        value = null;
        if (actualKey is null) return false;

        value = dictionary[actualKey];
        return true;

        static bool EqualityPredicate(Version x, Version y)
        {
            return VersionComparer.Instance.Equals(x, y);
        }

        static bool ComparisonPredicate(Version x, Version y)
        {
            var comparison = VersionComparer.Instance.Compare(x, y);
            return comparison <= 0;
        }
    }

    private static readonly Uri SMAPIToGameMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/smapi-game-versions.json");
    private static readonly Uri GameToSMAPIMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/game-smapi-versions.json");

    private SMAPIToGameMapping? _smapiToGameMappings;
    private GameToSMAPIMapping? _gameToSMAPIMappings;

    private async Task<SMAPIToGameMapping?> FetchSMAPIToGameMappings(CancellationToken cancellationToken)
    {
        if (_smapiToGameMappings is not null) return _smapiToGameMappings;

        var data = await FetchData<Dictionary<string, GameVersionsDTO>>(SMAPIToGameMappingsDataUri, cancellationToken);
        if (data is null) return null;

        var res = new Dictionary<Version, GameVersions>();
        foreach (var kv in data)
        {
            var (sVersion, gameVersions) = kv;
            var (sMinimumGameVersion, sMaximumGameVersion) = gameVersions;

            if (!TryParseVersion(sVersion, out var version)) continue;

            Version? minimumGameVersion = null, maximumGameVersion = null;
            if (sMinimumGameVersion is not null) if (!TryParseVersion(sMinimumGameVersion, out minimumGameVersion)) continue;
            if (sMaximumGameVersion is not null) if (!TryParseVersion(sMaximumGameVersion, out maximumGameVersion)) continue;

            res[version] = new GameVersions
            {
                MinimumGameVersion = minimumGameVersion,
                MaximumGameVersion = maximumGameVersion,
            };
        }

        _smapiToGameMappings = res.ToImmutableDictionary(keyComparer: VersionComparer.Instance);
        return _smapiToGameMappings;

    }

    private async Task<GameToSMAPIMapping?> FetchGameToSMAPIMappings(CancellationToken cancellationToken)
    {
        if (_gameToSMAPIMappings is not null) return _gameToSMAPIMappings;

        var data = await FetchData<Dictionary<string, string[]>>(GameToSMAPIMappingsDataUri, cancellationToken);
        if (data is null) return null;

        var res = new Dictionary<Version, Version>();
        foreach (var kv in data)
        {
            var (sGameVersion, smapiVersions) = kv;
            if (smapiVersions.Length == 0) continue;

            if (!TryParseVersion(sGameVersion, out var gameVersion)) continue;

            var sSMAPIVersion = smapiVersions.First();
            if (!TryParseVersion(sSMAPIVersion, out var smapiVersion)) continue;

            res[gameVersion] = smapiVersion;
        }

        _gameToSMAPIMappings = res.ToImmutableDictionary(keyComparer: VersionComparer.Instance);
        return _gameToSMAPIMappings;
    }

    private bool TryParseVersion(string input, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        if (Version.TryParse(input, out var tmp))
        {
            version = SimplifyVersion(tmp);
            return true;
        }

        _logger.LogWarning("Unable to parse string `{Input}` as a Version", input);
        return false;
    }

    private static Version SimplifyVersion(Version input)
    {
        var major = input.Major;
        var minor = input.Minor;
        var build = input.Build == -1 ? 0 : input.Build;
        var revision = input.Revision == 0 ? -1 : input.Revision;

        if (revision != -1) return new Version(major, minor, build, revision);
        return new Version(major, minor, build);
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

    private class VersionComparer : IEqualityComparer<Version>, IComparer<Version>
    {
        public static readonly VersionComparer Instance = new();

        public int GetHashCode(Version version)
        {
            // NOTE(erri120): used by dictionary lookups
            return version.GetHashCode();
        }

        public bool Equals(Version? x, Version? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;

            if (x.Major != y.Major) return false;
            if (x.Minor != y.Minor) return false;

            // NOTE(erri120): normalize Version and replace -1 with 0
            var xBuild = x.Build == -1 ? 0 : x.Build;
            var yBuild = y.Build == -1 ? 0 : y.Build;

            // NOTE(erri120): skipping revision because it's not part of a "semantic version"
            return xBuild == yBuild;
        }

        public int Compare(Version? x, Version? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var majorComparison = x.Major.CompareTo(y.Major);
            if (majorComparison != 0) return majorComparison;

            var minorComparison = x.Minor.CompareTo(y.Minor);
            if (minorComparison != 0) return minorComparison;

            // NOTE(erri120): normalize Version and replace -1 with 0
            var xBuild = x.Build == -1 ? 0 : x.Build;
            var yBuild = y.Build == -1 ? 0 : y.Build;

            // NOTE(erri120): skipping revision because it's not part of a "semantic version"
            return xBuild.CompareTo(yBuild);
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record GameVersions
    {
        public required Version? MinimumGameVersion { get; init; }

        public required Version? MaximumGameVersion { get; init; }

        public void Deconstruct(out Version? minimumGameVersion, out Version? maximumGameVersion)
        {
            minimumGameVersion = MinimumGameVersion;
            maximumGameVersion = MaximumGameVersion;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record GameVersionsDTO
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
