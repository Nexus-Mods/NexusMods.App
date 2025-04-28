using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;

namespace NexusMods.Games.StardewValley.Emitters;

using SMAPIToGameMapping = ImmutableDictionary<ISemanticVersion, SMAPIGameVersionDiagnosticEmitter.GameVersions>;
using GameToSMAPIMapping = ImmutableDictionary<ISemanticVersion, ISemanticVersion>;

[UsedImplicitly]
public class SMAPIGameVersionDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    public SMAPIGameVersionDiagnosticEmitter(
        ILogger<SMAPIGameVersionDiagnosticEmitter> logger,
        HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var smapiToGameMappings = await FetchSMAPIToGameMappings(cancellationToken);
        if (smapiToGameMappings is null) yield break;

        var gameToSMAPIMappings = await FetchGameToSMAPIMappings(cancellationToken);
        if (gameToSMAPIMappings is null) yield break;

        var gameVersion = Helpers.GetGameVersion(loadout);
        // var gameVersion = new SemanticVersion("1.6.12");

        if (!Helpers.TryGetSMAPI(loadout, out var smapi))
        {
            var smapiModCount = loadout
                .Items
                .OfTypeLoadoutItemGroup()
                .OfTypeSMAPIModLoadoutItem()
                .Count();

            // NOTE(erri120): The MissingSMAPIEmitter will warn the user if SMAPI is required.
            // This emitter will suggest SMAPI if there are no mods yet.
            if (smapiModCount != 0) yield break;

            var suggestion = SuggestSMAPI(gameToSMAPIMappings, gameVersion);
            if (suggestion is not null) yield return suggestion;
            yield break;
        }

        if (!SMAPILoadoutItem.Version.TryGetValue(smapi, out var smapiVersionString))
        {
            _logger.LogError("Unable to get the version of the SMAPI mod");
            yield break;
        }

        if (!SemanticVersion.TryParse(smapiVersionString, out var smapiVersion))
        {
            _logger.LogError("Unable to parse `{Version}` as a semantic version", smapiVersionString);
            yield break;
        }

        // var smapiVersion = new SemanticVersion("4.1.10");

        if (!TryGetValue(smapiToGameMappings, smapiVersion, useEquals: true, out var supportedGameVersions))
        {
            // ReSharper disable once InconsistentLogPropertyNaming
            _logger.LogWarning("Found no game version information for SMAPI version {SMAPIVersion}", smapiVersion);
            yield break;
        }

        var diagnostic1 = GameVersionOlderThanMinimumGameVersion(
            gameToSMAPIMappings,
            loadout, smapi,
            gameVersion, smapiVersion, supportedGameVersions
        );

        var diagnostic2 = GameVersionNewerThanMaximumGameVersion(
            gameToSMAPIMappings,
            loadout, smapi,
            gameVersion, smapiVersion, supportedGameVersions
        );

        if (diagnostic1 is not null) yield return diagnostic1;
        if (diagnostic2 is not null) yield return diagnostic2;
    }

    private Diagnostic? SuggestSMAPI(GameToSMAPIMapping gameToSMAPIMappings, ISemanticVersion gameVersion)
    {
        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: false, out var supportedSMAPIVersion))
        {
            _logger.LogWarning("Found details for game version {GameVersion}", gameVersion);
            return null;
        }

        return Diagnostics.CreateSuggestSMAPIVersion(
            LatestSMAPIVersion: supportedSMAPIVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            SMAPINexusModsLink: Helpers.SMAPILink
        );
    }

    private static readonly NamedLink GitHubDataLink = new("GitHub", new Uri("https://github.com/erri120/smapi-versions/blob/main/data/smapi-game-versions.json"));

    private Diagnostic? GameVersionNewerThanMaximumGameVersion(
        GameToSMAPIMapping gameToSMAPIMappings,
        Loadout.ReadOnly loadout,
        SMAPILoadoutItem.ReadOnly smapi,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        GameVersions supportedGameVersions)
    {
        var (_, maximumGameVersion) = supportedGameVersions;
        if (maximumGameVersion is null) return null;

        var comparison = maximumGameVersion.CompareTo(gameVersion);
        if (comparison >= 0) return null;

        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: false, out var supportedSMAPIVersion))
        {
            if (!TryGetLastSupportedSMAPIVersion(gameToSMAPIMappings, gameVersion, out supportedSMAPIVersion))
            {
                _logger.LogWarning("No data to recommend latest supported SMAPI version for `{GameVersion}`", gameVersion);
                return null;
            }
        }

        return Diagnostics.CreateGameVersionNewerThanMaximumGameVersion(
            SMAPI: smapi.AsLoadoutItemGroup().ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MaximumGameVersion: maximumGameVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            NewestSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersion.ToString(),
            SMAPINexusModsLink: Helpers.SMAPILink,
            GitHubData: GitHubDataLink
        );
    }

    private Diagnostic? GameVersionOlderThanMinimumGameVersion(
        GameToSMAPIMapping gameToSMAPIMappings,
        Loadout.ReadOnly loadout,
        SMAPILoadoutItem.ReadOnly smapi,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        GameVersions supportedGameVersions)
    {
        var (minimumGameVersion, _) = supportedGameVersions;
        if (minimumGameVersion is null) return null;

        var comparison = minimumGameVersion.CompareTo(gameVersion);
        if (comparison <= 0) return null;

        if (!TryGetValue(gameToSMAPIMappings, gameVersion, useEquals: true, out var supportedSMAPIVersion))
        {
            if (!TryGetLastSupportedSMAPIVersion(gameToSMAPIMappings, gameVersion, out supportedSMAPIVersion))
            {
                _logger.LogWarning("No data to recommend latest supported SMAPI version for `{GameVersion}`", gameVersion);
                return null;
            }
        }

        return Diagnostics.CreateGameVersionOlderThanMinimumGameVersion(
            SMAPI: smapi.AsLoadoutItemGroup().ToReference(loadout),
            SMAPIVersion: smapiVersion.ToString(),
            MinimumGameVersion: minimumGameVersion.ToString(),
            CurrentGameVersion: gameVersion.ToString(),
            NewestSupportedSMAPIVersionForCurrentGameVersion: supportedSMAPIVersion.ToString(),
            SMAPINexusModsLink: Helpers.SMAPILink,
            GitHubData: GitHubDataLink
        );
    }

    /// <summary>
    /// Returns the latest supported SMAPI version for <paramref name="gameVersion"/>.
    /// </summary>
    internal static bool TryGetLastSupportedSMAPIVersion(
        GameToSMAPIMapping gameToSmapiMappings,
        ISemanticVersion gameVersion,
        [NotNullWhen(true)] out ISemanticVersion? supportedSMAPIVersion)
    {
        var found = gameToSmapiMappings
            .OrderByDescending(static kv => kv.Key)
            .SkipWhile(current => current.Key.CompareTo(gameVersion) > 0)
            .TryGetFirst(out var mapping);

        if (!found)
        {
            supportedSMAPIVersion = null;
            return false;
        }

        supportedSMAPIVersion = mapping.Value;
        return true;
    }

    private static bool TryGetValue<T>(
        ImmutableDictionary<ISemanticVersion, T> dictionary,
        ISemanticVersion input,
        bool useEquals,
        [NotNullWhen(true)] out T? value) where T : class
    {
        var actualKey = dictionary
            .Keys
            .OrderDescending()
            .FirstOrDefault(x => useEquals
                ? EqualityPredicate(x, input)
                : ComparisonPredicate(x, input)
            );

        value = null;
        if (actualKey is null) return false;

        value = dictionary[actualKey];
        return true;

        static bool EqualityPredicate(ISemanticVersion x, ISemanticVersion y)
        {
            return x.Equals(y);
        }

        static bool ComparisonPredicate(ISemanticVersion x, ISemanticVersion y)
        {
            var comparison = x.CompareTo(y);
            return comparison <= 0;
        }
    }

    private static readonly Uri SMAPIToGameMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/smapi-game-versions.json");
    private static readonly Uri GameToSMAPIMappingsDataUri = new("https://github.com/erri120/smapi-versions/raw/main/data/game-smapi-versions.json");

    private SMAPIToGameMapping? _smapiToGameMappings;
    private GameToSMAPIMapping? _gameToSMAPIMappings;

    internal async Task<SMAPIToGameMapping?> FetchSMAPIToGameMappings(CancellationToken cancellationToken)
    {
        if (_smapiToGameMappings is not null) return _smapiToGameMappings;

        var data = await FetchData<Dictionary<string, GameVersionsDTO>>(SMAPIToGameMappingsDataUri, cancellationToken);
        if (data is null) return null;

        var res = new Dictionary<ISemanticVersion, GameVersions>();
        foreach (var kv in data)
        {
            var (sVersion, gameVersions) = kv;
            var (sMinimumGameVersion, sMaximumGameVersion) = gameVersions;

            if (!TryParseVersion(sVersion, out var version)) continue;

            ISemanticVersion? minimumGameVersion = null, maximumGameVersion = null;
            if (sMinimumGameVersion is not null) if (!TryParseVersion(sMinimumGameVersion, out minimumGameVersion)) continue;
            if (sMaximumGameVersion is not null) if (!TryParseVersion(sMaximumGameVersion, out maximumGameVersion)) continue;

            res[version] = new GameVersions
            {
                MinimumGameVersion = minimumGameVersion,
                MaximumGameVersion = maximumGameVersion,
            };
        }

        _smapiToGameMappings = res.ToImmutableDictionary();
        return _smapiToGameMappings;

    }

    internal async Task<GameToSMAPIMapping?> FetchGameToSMAPIMappings(CancellationToken cancellationToken)
    {
        if (_gameToSMAPIMappings is not null) return _gameToSMAPIMappings;

        var data = await FetchData<Dictionary<string, string[]>>(GameToSMAPIMappingsDataUri, cancellationToken);
        if (data is null) return null;

        var res = new Dictionary<ISemanticVersion, ISemanticVersion>();
        foreach (var kv in data)
        {
            var (sGameVersion, smapiVersions) = kv;
            if (smapiVersions.Length == 0) continue;

            if (!TryParseVersion(sGameVersion, out var gameVersion)) continue;

            var sSMAPIVersion = smapiVersions.First();
            if (!TryParseVersion(sSMAPIVersion, out var smapiVersion)) continue;

            res[gameVersion] = smapiVersion;
        }

        _gameToSMAPIMappings = res.ToImmutableDictionary();
        return _gameToSMAPIMappings;
    }

    private bool TryParseVersion(string input, [NotNullWhen(true)] out ISemanticVersion? version)
    {
        version = null;
        if (SemanticVersion.TryParse(input, out version)) return true;

        _logger.LogWarning("Unable to parse string `{Input}` as a SemanticVersion", input);
        return false;
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
            _logger.LogWarning(e, "Exception while getting JSON data from {Uri}", dataUri);
            return null;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record GameVersions
    {
        public required ISemanticVersion? MinimumGameVersion { get; init; }

        public required ISemanticVersion? MaximumGameVersion { get; init; }

        public void Deconstruct(out ISemanticVersion? minimumGameVersion, out ISemanticVersion? maximumGameVersion)
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
