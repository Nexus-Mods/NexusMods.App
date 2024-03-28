using System.Collections.Immutable;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Paths;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Utilities;

namespace NexusMods.Games.StardewValley.WebAPI;

/// <summary>
/// Implementation of <see cref="ISMAPIWebApi"/> using <see cref="WebApiClient"/>
/// </summary>
internal sealed class SMAPIWebApi : ISMAPIWebApi
{
    private const string ApiBaseUrl = "https://smapi.io/api";
    private const string NexusModsBaseUrl = "https://nexusmods.com/stardewvalley/mods";

    private readonly ILogger<SMAPIWebApi> _logger;

    private bool _isDisposed;
    private WebApiClient? _client;

    private ImmutableDictionary<string, NamedLink> _knownModPageUrls = ImmutableDictionary<string, NamedLink>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    public SMAPIWebApi(ILogger<SMAPIWebApi> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, NamedLink>> GetModPageUrls(
        IOSInformation os,
        Version gameVersion,
        Version smapiVersion,
        string[] smapiIDs)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(SMAPIWebApi));

        var semanticGameVersion = new SemanticVersion(gameVersion);
        var semanticSMAPIVersion = new SemanticVersion(smapiVersion);
        var platform = ToPlatform(os);

        _client ??= new WebApiClient(
            baseUrl: ApiBaseUrl,
            version: semanticSMAPIVersion
        );

        var mods = smapiIDs
            .Where(id => !_knownModPageUrls.ContainsKey(id))
            .Select(id => new ModSearchEntryModel(
                    id: id,
                    installedVersion: null,
                    updateKeys: null,
                    isBroken: false
            ))
            .ToArray();

        if (mods.Length != 0)
        {
            IDictionary<string, ModEntryModel>? apiResult = null;

            try
            {
                apiResult = await _client.GetModInfoAsync(
                    mods: mods,
                    apiVersion: semanticSMAPIVersion,
                    gameVersion: semanticGameVersion,
                    platform: platform,
                    includeExtendedMetadata: true
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception contacting {Url}", ApiBaseUrl);
            }

            if (apiResult is not null)
            {
                var tmp = apiResult
                    .Select<KeyValuePair<string, ModEntryModel>, ValueTuple<string?, Optional<NamedLink>>>(kv =>
                    {
                        var (id, model) = kv;
                        var metadata = model.Metadata;

                        var nexusId = metadata?.NexusID;
                        if (nexusId is null) return (null, Optional<NamedLink>.None);

                        var uri = new Uri($"{NexusModsBaseUrl}/{nexusId.Value}");
                        return (id, uri.WithName("Nexus Mods"));
                    })
                    .Where(kv => kv.Item1 is not null)
                    .Select(tuple => new KeyValuePair<string, NamedLink>(tuple.Item1!, tuple.Item2.Value))
                    .ToDictionary();

                ImmutableDictionary<string, NamedLink> initial, updated;
                do
                {
                    initial = _knownModPageUrls;
                    updated = _knownModPageUrls.SetItems(tmp);
                } while (initial != Interlocked.CompareExchange(ref _knownModPageUrls, updated, initial));
            }
        }

        return smapiIDs
            .Select(id => (Id: id, Link: _knownModPageUrls.GetValueOrDefault(id)))
            .Where(tuple => tuple.Link != default(NamedLink))
            .Select(tuple => new KeyValuePair<string, NamedLink>(tuple.Id, tuple.Link))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static Platform ToPlatform(IOSInformation os)
    {
        return os.MatchPlatform(
            onWindows: () => Platform.Windows,
            onLinux: () => Platform.Linux,
            onOSX: () => Platform.Mac
        );
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(SMAPIWebApi));
        _isDisposed = true;

        try
        {
            _client?.Dispose();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
