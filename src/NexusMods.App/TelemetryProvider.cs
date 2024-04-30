using System.Reactive.Disposables;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI;
using NexusMods.Networking.NexusWebApi;
using OneOf;

namespace NexusMods.App;

internal class TelemetryProvider : ITelemetryProvider, IDisposable
{
    private readonly CompositeDisposable _disposable;

    public TelemetryProvider(IServiceProvider serviceProvider)
    {
        _disposable = new CompositeDisposable();

        var loginManager = serviceProvider.GetRequiredService<LoginManager>();
        loginManager.IsPremium.SubscribeWithErrorLogging(value => _isPremium = value).DisposeWith(_disposable);
    }

    public void ConfigureMetrics(IMeterConfig meterConfig)
    {
        meterConfig.CreateActiveUsersCounter();
        meterConfig.CreateUsersPerOSCounter();
        meterConfig.CreateUsersPerLanguageCounter();
        meterConfig.CreateUsersPerMembershipCounter(GetMembership);
    }

    private bool _isPremium;
    private OneOf<MembershipStatus.None, MembershipStatus.Premium> GetMembership()
    {
        return _isPremium ? new MembershipStatus.Premium() : new MembershipStatus.None();
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
