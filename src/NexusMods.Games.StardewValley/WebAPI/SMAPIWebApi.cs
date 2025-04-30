using System.Collections.Immutable;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Paths;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Utilities;

namespace NexusMods.Games.StardewValley.WebAPI;

/// <summary>
/// Implementation of <see cref="ISMAPIWebApi"/> using <see cref="WebApiClient"/>
/// </summary>
internal sealed class SMAPIWebApi : ISMAPIWebApi
{
    private const string ApiBaseUrl = "https://smapi.io/api";

    private readonly ILogger<SMAPIWebApi> _logger;

    private bool _isDisposed;
    private WebApiClient? _client;

    private ImmutableDictionary<string, SMAPIWebApiMod> _cache = ImmutableDictionary<string, SMAPIWebApiMod>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    public SMAPIWebApi(ILogger<SMAPIWebApi> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, SMAPIWebApiMod>> GetModDetails(
        IOSInformation os,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        string[] smapiIDs)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(SMAPIWebApi));

        var platform = ToPlatform(os);

        _client ??= new WebApiClient(
            baseUrl: ApiBaseUrl,
            version: smapiVersion
        );

        var mods = smapiIDs
            .Where(id => !_cache.ContainsKey(id))
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
                    apiVersion: smapiVersion,
                    gameVersion: gameVersion,
                    platform: platform,
                    includeExtendedMetadata: true
                );
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception contacting {Url}", ApiBaseUrl);
            }

            if (apiResult is not null)
            {
                var tmp = apiResult
                    .Select(kv =>
                    {
                        var (id, model) = kv;
                        var metadata = model.Metadata;

                        var nexusId = metadata?.NexusID;
                        var nexusModsLink = Optional<NamedLink>.None;

                        if (nexusId is not null)
                        {
                            var uri = NexusModsUrlBuilder.GetModUri(StardewValley.DomainStatic, ModId.From((uint)nexusId.Value));
                            nexusModsLink = uri.WithName("Nexus Mods");
                        }

                        return new SMAPIWebApiMod
                        {
                            UniqueId = id,
                            Name = metadata?.Name,
                            NexusModsLink = nexusModsLink,
                        };
                    })
                    .ToDictionary(x => x.UniqueId, x => x, StringComparer.OrdinalIgnoreCase);

                ImmutableDictionary<string, SMAPIWebApiMod> initial, updated;
                do
                {
                    initial = _cache;
                    updated = _cache.SetItems(tmp);
                } while (initial != Interlocked.CompareExchange(ref _cache, updated, initial));
            }
        }

        return smapiIDs
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => _cache.GetValueOrDefault(id))
            .Where(mod => mod is not null)
            .Select(mod => mod!)
            .ToDictionary(mod => mod.UniqueId, mod => mod, StringComparer.OrdinalIgnoreCase);
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
