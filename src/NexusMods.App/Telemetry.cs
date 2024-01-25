using System.Diagnostics.Metrics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Telemetry;
using NexusMods.Telemetry.Metrics;

namespace NexusMods.App;

internal static class Telemetry
{
    private const string AssemblyName = "NexusMods.App";
    // TODO: https://github.com/Nexus-Mods/NexusMods.App/pull/644
    private static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 1);
    private static readonly string AssemblyVersionString = AssemblyVersion.ToString(fieldCount: 3);

    internal static readonly TelemetryLibraryInfo LibraryInfo = new()
    {
        AssemblyName = AssemblyName,
        AssemblyVersion = AssemblyVersion
    };

    private static readonly Meter Meter = new(name: AssemblyName, version: AssemblyVersionString);

    [UsedImplicitly]
    private static DIAwareMetricManager? _metricManager;
    internal static void SetupTelemetry(IServiceProvider serviceProvider)
    {
        _metricManager = new DIAwareMetricManager(serviceProvider);
    }

    [UsedImplicitly]
    private static readonly ObservableUpDownCounter<int> ActiveUsersCounter = Meter.CreateActiveUsersCounter();

    [UsedImplicitly]
    private static readonly ObservableUpDownCounter<int> OperatingSystemCounter = Meter.CreateOperatingSystemCounter();

    private class DIAwareMetricManager
    {
        private bool _isPremium;

        [UsedImplicitly]
        private ObservableUpDownCounter<int> _membershipCounter;

        public DIAwareMetricManager(IServiceProvider serviceProvider)
        {
            var loginManager = serviceProvider.GetRequiredService<LoginManager>();
            loginManager.IsPremium.SubscribeWithErrorLogging(value => _isPremium = value);

            _membershipCounter = Meter.CreateMembershipCounter(this, state => state._isPremium ? "premium" : "member");
        }
    }
}
