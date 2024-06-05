using System.Reactive.Disposables;
using DynamicData.Aggregation;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.App;

internal sealed class TelemetryProvider : ITelemetryProvider, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly IRepository<Loadout.Model> _loadoutRepository;

    public TelemetryProvider(IServiceProvider serviceProvider)
    {
        _loadoutRepository = serviceProvider.GetRequiredService<IRepository<Loadout.Model>>();

        // membership status
        var loginManager = serviceProvider.GetRequiredService<LoginManager>();
        loginManager.IsPremiumObservable.SubscribeWithErrorLogging(value => _isPremium = value).DisposeWith(_disposable);

        // download size
        var downloadAnalysisRepository = serviceProvider.GetRequiredService<IRepository<DownloadAnalysis.Model>>();
        downloadAnalysisRepository
            .Observable
            .ToObservableChangeSet()
            .Sum(x => (double)x.Size.Value)
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
        return _loadoutRepository.All
            .Where(x => x.IsVisible())
            .Select(x => x.Installation.Game.Domain)
            .Distinct()
            .Count();
    }

    private Counters.LoadoutModCount[] GetModsPerLoadout()
    {
        return _loadoutRepository.All
            .Where(x => x.IsVisible())
            .Select(x =>
            {
                var count = x.Mods.Count(mod => mod.Category == ModCategory.Mod);
                return new Counters.LoadoutModCount(x.Installation.Game.Domain, count);
            })
            .ToArray();
    }

    private Size _downloadSize = Size.Zero;
    private Size GetDownloadSize() => _downloadSize;

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
