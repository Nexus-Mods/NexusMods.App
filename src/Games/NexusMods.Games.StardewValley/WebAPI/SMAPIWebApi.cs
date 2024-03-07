using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
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

    private ImmutableDictionary<string, Uri> _knownModPageUrls = ImmutableDictionary<string, Uri>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    public SMAPIWebApi(ILogger<SMAPIWebApi> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, Uri>> GetModPageUrls(
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
                    .Select(kv =>
                    {
                        var (id, model) = kv;
                        var metadata = model.Metadata;

                        var nexusId = metadata?.NexusID;
                        if (nexusId is null) return (null, null);

                        var uri = new Uri($"{NexusModsBaseUrl}/{nexusId.Value}");
                        return (id, uri);
                    })
                    .Where(kv => kv.id is not null)
                    .Select(tuple => new KeyValuePair<string, Uri>(tuple.id!, tuple.uri!))
                    .ToDictionary();

                ImmutableDictionary<string, Uri> initial, updated;
                do
                {
                    initial = _knownModPageUrls;
                    updated = _knownModPageUrls.SetItems(tmp);
                } while (initial != Interlocked.CompareExchange(ref _knownModPageUrls, updated, initial));
            }
        }

        return smapiIDs
            .Select(id => (Id: id, Uri: _knownModPageUrls.GetValueOrDefault(id)))
            .Where(tuple => tuple.Uri is not null)
            .Select(tuple => new KeyValuePair<string, Uri>(tuple.Id, tuple.Uri!))
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
