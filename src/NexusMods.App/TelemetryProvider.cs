using System.Reactive.Disposables;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
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
    }

    private bool _isPremium;
    private OneOf<MembershipStatus.None, MembershipStatus.Premium> GetMembership()
    {
        return _isPremium ? new MembershipStatus.Premium() : new MembershipStatus.None();
    }

    private int GetManagedGamesCount()
    {
        return _loadoutRepository.All
            .Select(x => x.Installation.Game.Domain)
            .Distinct()
            .Count();
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
