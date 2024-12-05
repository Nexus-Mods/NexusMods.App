using System.Reactive.Disposables;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.App.Common;

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
            .Select(x => x.Installation.GameId)
            .Distinct()
            .Count();
    }

    private Counters.LoadoutModCount[] GetModsPerLoadout()
    {
        var db = _connection.Db;
        var dict = db
            .Datoms(LibraryLinkedLoadoutItem.PrimaryAttribute)
            .Select(datom => LoadoutItem.Load(db, datom.E))
            .GroupBy(static item => item.LoadoutId)
            .ToDictionary(static grouping => grouping.Key, static grouping => grouping.Count());

        return Loadout.All(db)
            .Where(static loadout => loadout.IsVisible())
            .Select(loadout => new Counters.LoadoutModCount(loadout.Installation.Name, dict[loadout.LoadoutId]))
            .ToArray();
    }

    private Size _downloadSize = Size.Zero;

    private Size GetDownloadSize() => _downloadSize;

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
