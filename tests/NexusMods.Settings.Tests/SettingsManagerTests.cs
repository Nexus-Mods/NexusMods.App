using FluentAssertions;
using NexusMods.Abstractions.Settings;
using Xunit;

namespace NexusMods.Settings.Tests;

public partial class SettingsManagerTests
{
    [Fact]
    public void TestGet()
    {
        var settingsManager = Setup();
        var res = settingsManager.Get<MySettings>();
        res.Foo.Should().BeFalse();
        res.Bar.Should().Be("Bar");
    }

    [Fact]
    public void TestSet()
    {
        var settingsManager = Setup();

        var observedChanges = new List<MySettings>();
        var observable = settingsManager.GetChanges<MySettings>();
        observable.Subscribe(changed =>
        {
            observedChanges.Add(changed with {});
        });

        settingsManager.Get<MySettings>().Bar.Should().Be("Bar");
        observedChanges.Should().ContainSingle();

        settingsManager.Update<MySettings>(settings => settings with
        {
            Bar = "Foo",
        });
        observedChanges.Should().HaveCount(2);

        settingsManager.Get<MySettings>().Bar.Should().Be("Foo");
    }

    [Fact]
    public void TestSaveLoad()
    {
        var storageBackend = new InMemoryStorageBackend();

        var settingsManager1 = Setup(storageBackend);
        settingsManager1.Get<MySettings>().Bar.Should().Be("Bar");
        settingsManager1.Update<MySettings>(settings => settings with
        {
            Bar = "Foo",
        });

        var settingsManager2 = Setup(storageBackend);
        settingsManager2.Get<MySettings>().Bar.Should().Be("Foo");
    }

    [Fact]
    public void TestOverride()
    {
        var storageBackend = new InMemoryStorageBackend();
        var overrideInformation = new SettingsOverrideInformation(typeof(MySettings), obj =>
        {
            var setting = obj.Should().BeOfType<MySettings>().Subject;
            setting.Bar = "Baz";
            return setting;
        });

        var settingsManager1 = Setup(storageBackend, overrideInformation);
        settingsManager1.Get<MySettings>().Bar.Should().Be("Baz");

        var settingsManager2 = Setup(storageBackend);
        settingsManager2.Get<MySettings>().Bar.Should().Be("Bar");
    }
}
