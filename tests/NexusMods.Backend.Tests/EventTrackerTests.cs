using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Backend.Tracking;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using NSubstitute;

namespace NexusMods.Backend.Tests;

public class EventTrackerTests
{
    private EventTracker Setup()
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

        var firstEvent = new EventDefinition("my first event")
        {
            EventPropertyDefinition.Create<int>("foo"),
            EventPropertyDefinition.Create<int>("bar"),
            EventPropertyDefinition.Create<int>("baz"),
        };

        var secondEvent = new EventDefinition("my second event")
        {
            EventPropertyDefinition.Create<string>("a"),
            EventPropertyDefinition.Create<string>("b"),
            EventPropertyDefinition.Create<string>("c"),
            EventPropertyDefinition.Create<string>("d", isOptional: true),
            EventPropertyDefinition.Create<int>("e", isOptional: true),
        };

        tracker.Track(firstEvent, ("foo", 1), ("bar", 2), ("baz", 3));
        tracker.Track(secondEvent, ("a", "a"), ("b", "b"), ("c", "c"));
        tracker.Track(secondEvent, ("a", "a"), ("b", "b"), ("c", "c"), ("d", (string?)null));
        tracker.Track(secondEvent, ("a", "a"), ("b", "b"), ("c", "c"), ("e", (int?)null));
        tracker.Track(secondEvent, ("a", "a"), ("b", "b"), ("c", "c"), ("e", 1));

        using var buffer = tracker.PrepareRequest();
        await Assert.That(buffer).IsNotNull();

        var json = Encoding.UTF8.GetString(buffer!.WrittenSpan);
        await VerifyJson(json);
    }

    [Test]
    public async Task Test_EventPropertyValidation()
    {
        var tracker = Setup();

        var eventDefinition = new EventDefinition("example")
        {
            EventPropertyDefinition.Create<int>("foo"),
            EventPropertyDefinition.Create<string>("bar"),
            EventPropertyDefinition.Create<int>("baz", isOptional: true),
        };

        void Act1() => tracker.Track(eventDefinition, ("foo", 1));
        await Assert.That(Act1).ThrowsExactly<InvalidOperationException>().WithMessage("Missing required property `bar` on event `example`");

        void Act2() => tracker.Track(eventDefinition, ("foo", 1), ("foo", 2), ("bar", "baz"));
        await Assert.That(Act2).ThrowsExactly<InvalidOperationException>().WithMessage("Property `foo` has already been added to the event `example`");

        void Act3() => tracker.Track(eventDefinition, ("foo", 1), ("bar", 2));
        await Assert.That(Act3).ThrowsExactly<InvalidOperationException>().WithMessage("Property definition type mismatch for property `bar` on event `example`: expected `System.String` but received `System.Int32`");

        void Act4() => tracker.Track(eventDefinition, ("foo", 1), ("bar", "bar"), ("random", 1));
        await Assert.That(Act4).ThrowsExactly<InvalidOperationException>().WithMessage("Event definition `example` doesn't contain a property definition for `random`");
    }
}
