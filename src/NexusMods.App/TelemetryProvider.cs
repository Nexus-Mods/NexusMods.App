using System.Collections.Concurrent;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.App.UI.Pages;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.App;

internal sealed class TelemetryProvider : ITelemetryProvider, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly IConnection _connection;

    public TelemetryProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        
        // membership status
        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        loginManager.IsPremiumObservable.SubscribeWithErrorLogging(value => _isPremium = value).DisposeWith(_disposable);

        // download size
        _connection.ObserveDatoms(LibraryFile.Size)
            .Transform(d => (SizeAttribute.ReadDatom)d.Resolved(_connection))
            .RemoveKey()
            .QueryWhenChanged(datoms => datoms.Sum(d => d.V))
            .SubscribeWithErrorLogging(totalDownloadSize => _downloadSize = Size.From((ulong)totalDownloadSize))
            .DisposeWith(_disposable);

        Loadout
            .ObserveAll(_connection)
            .FilterImmutable(static loadout => loadout.IsVisible())
            .TransformOnObservable(loadout => LoadoutDataProviderHelper.CountAllLoadoutItems(serviceProvider, loadout.LoadoutId))
            .QueryWhenChanged(query =>
            {
                _loadoutCounts.Clear();

                foreach (var kv in query.KeyValues)
                {
                    _loadoutCounts[kv.Key] = kv.Value;
                }

                return 0;
            })
            .SubscribeWithErrorLogging()
            .DisposeWith(_disposable);
    }

    public void ConfigureMetrics(IMeterConfig meterConfig)
    {
        meterConfig.CreateActiveUsersPerVersionCounter(ApplicationConstants.Version);
        meterConfig.CreateUsersPerOSCounter();
        meterConfig.CreateUsersPerLanguageCounter();
        meterConfig.CreateUsersPerMembershipCounter(GetMembership);
        meterConfig.CreateManagedGamesCounter(GetManagedGamesCount);
        meterConfig.CreateModsPerGameCounter(GetModsPerLoadout);
        meterConfig.CreateGlobalDownloadSizeCounter(GetDownloadSize);
    }

    private bool _isPremium;
    private OneOf<MembershipStatus.None, MembershipStatus.Premium> GetMembership()
    {
        return _isPremium ? new MembershipStatus.Premium() : new MembershipStatus.None();
    }

    private int GetManagedGamesCount()
    {
        return Loadout.All(_connection.Db)
            .Where(x => x.IsVisible())
            .DistinctBy(static x => x.Installation.GameId)
            .Count();
    }

    private readonly ConcurrentDictionary<EntityId, int> _loadoutCounts = [];
    private Counters.LoadoutModCount[] GetModsPerLoadout()
    {
        return _loadoutCounts.Select(kv =>
        {
            var loadout = Loadout.Load(_connection.Db, kv.Key);
            if (!loadout.IsValid()) return Optional<Counters.LoadoutModCount>.None;

            return new Counters.LoadoutModCount(loadout.Installation.Name, kv.Value);
        }).Where(static optional => optional.HasValue).Select(static optional => optional.Value).ToArray();
    }

    private Size _downloadSize = Size.Zero;

    private Size GetDownloadSize() => _downloadSize;

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
