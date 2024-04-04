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

        // var gameVersion = SimplifyVersion(new Version("1.5.6"));
        var gameVersion = SimplifyVersion(loadout.Installation.Version);

        var smapiMod = loadout.Mods
            .Where(kv => kv.Value.Enabled)
            .Select(kv => kv.Value)
            .FirstOrDefault(mod => mod.Metadata.OfType<SMAPIMarker>().Any());

        if (smapiMod is null) yield break;
        var smapiMarker = smapiMod.Metadata.OfType<SMAPIMarker>().First();
        var smapiVersion = SimplifyVersion(smapiMarker.Version!);

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
        if (!smapiToGameMappings.TryGetValue(smapiVersion, out var supportedGameVersions))
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogWarning("Found no game version information for SMAPI version {SMAPIVersion}", smapiVersion);
            return null;
        }

        var (minimumGameVersion, _) = supportedGameVersions;
        if (minimumGameVersion is null) return null;

        if (minimumGameVersion <= gameVersion) return null;

        if (!gameToSMAPIMappings.TryGetValue(gameVersion, out var supportedSMAPIVersion))
        {
            _logger.LogWarning("Found no supported SMAPI versions for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateGameVersionOlderThanMinimumGameVersion(
            SMAPIMod: smapiMod.ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MinimumGameVersion: minimumGameVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            LastSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersion.ToString(),
            SMAPINexusModsLink: NexusModsSMAPILink
        );
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

    private class VersionComparer : IEqualityComparer<Version>
    {
        public static readonly IEqualityComparer<Version> Instance = new VersionComparer();

        public bool Equals(Version? x, Version? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Equals(y);
        }

        public int GetHashCode(Version version)
        {
            return version.GetHashCode();
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
