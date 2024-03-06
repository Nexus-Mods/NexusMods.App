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

    private readonly Dictionary<string, Uri> _knownModPageUrls = new(StringComparer.OrdinalIgnoreCase);

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
                )
            )
            .ToArray();

        if (mods.Length != 0)
        {
            try
            {
                var res = await _client.GetModInfoAsync(
                    mods: mods,
                    apiVersion: semanticSMAPIVersion,
                    gameVersion: semanticGameVersion,
                    platform: platform,
                    includeExtendedMetadata: true
                );

                foreach (var kv in res)
                {
                    var (id, model) = kv;
                    var metadata = model.Metadata;
                    if (metadata is null) continue;

                    var nexusId = metadata.NexusID;
                    if (nexusId is null) continue;

                    var uri = new Uri($"{NexusModsBaseUrl}/{nexusId.Value}");
                    _knownModPageUrls[id] = uri;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception contacting {Url}", ApiBaseUrl);
            }
        }

        return smapiIDs
            .Select(id => _knownModPageUrls.TryGetValue(id, out var uri) ? (id, uri) : (id, null))
            .Where(kv => kv.uri is not null)
            .ToDictionary(kv => kv.id, kv => kv.uri!, StringComparer.OrdinalIgnoreCase);
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
