using System.Reactive.Disposables;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI;
using NexusMods.Networking.NexusWebApi;
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
        loginManager.IsPremium.SubscribeWithErrorLogging(value => _isPremium = value).DisposeWith(_disposable);
    }

    public void ConfigureMetrics(IMeterConfig meterConfig)
    {
        meterConfig.CreateActiveUsersCounter();
        meterConfig.CreateUsersPerOSCounter();
        meterConfig.CreateUsersPerLanguageCounter();
        meterConfig.CreateUsersPerMembershipCounter(GetMembership);
        meterConfig.CreateManagedGamesCounter(GetManagedGamesCount);
        meterConfig.CreateModsPerGameCounter(GetModsPerLoadout);
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

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
