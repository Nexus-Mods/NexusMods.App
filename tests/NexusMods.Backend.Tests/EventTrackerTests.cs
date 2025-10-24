using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Backend.Tracking;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using NSubstitute;

namespace NexusMods.Backend.Tests;

public class EventTrackerTests
{
    private static EventTracker Setup()
    {
        var timeProvider = new FakeTimeProvider(startDateTime: DateTimeOffset.UnixEpoch);
        var loginManager = Substitute.For<ILoginManager>();
        var settingsManager = Substitute.For<ISettingsManager>();

        settingsManager.Get<TrackingSettings>().Returns(new TrackingSettings
        {
            DeviceId = Guid.Empty,
            EnableTracking = true,
        });

        var tracker = new EventTracker(
            logger: NullLogger<EventTracker>.Instance,
            timeProvider: timeProvider,
            osInformation: OSInformation.FakeUnix,
            loginManager: loginManager,
            settingsManager: settingsManager,
            jsonSerializerOptions: new JsonSerializerOptions()
        );

        return tracker;
    }
    
    [Test]
    public async Task Test_PrepareRequest()
    {
        var tracker = Setup();

        tracker.Track("my first event", ("foo", 1), ("bar", 2), ("baz", 3));
        tracker.Track("my second event", ("a", "a"), ("b", "b"), ("c", "c"));
        tracker.Track("my second event", ("a", "a"), ("b", "b"), ("c", "c"), ("d", (string?)null));
        tracker.Track("my second event", ("a", "a"), ("b", "b"), ("c", "c"), ("e", (int?)null));
        tracker.Track("my second event", ("a", "a"), ("b", "b"), ("c", "c"), ("e", 1));

        using var buffer = tracker.PrepareRequest();
        await Assert.That(buffer).IsNotNull();

        var json = Encoding.UTF8.GetString(buffer!.WrittenSpan);
        await VerifyJson(json)
            .AddScrubber(s => s.Replace(
                ApplicationConstants.Version.ToSafeString(maxFieldCount: 3),
                "{{app_version}}"
            ));
    }
}
