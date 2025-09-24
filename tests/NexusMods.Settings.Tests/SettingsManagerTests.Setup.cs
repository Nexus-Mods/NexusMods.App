using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.Abstractions.Settings;
using NSubstitute;
using Xunit;

namespace NexusMods.Settings.Tests;

public partial class SettingsManagerTests
{
    private static SettingsManager Setup(InMemoryStorageBackend? storageBackend = null, SettingsOverrideInformation? overrideInformation = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        serviceProvider.GetService(typeof(ILogger<SettingsManager>)).Returns(NullLogger<SettingsManager>.Instance);

        if (storageBackend is not null)
        {
            serviceProvider.GetService(typeof(DefaultSettingsStorageBackend)).Returns(new DefaultSettingsStorageBackend(storageBackend));
            serviceProvider.GetService(typeof(IEnumerable<IBaseSettingsStorageBackend>)).Returns(new IBaseSettingsStorageBackend[]
            {
                storageBackend,
            });
        }
        else
        {
            serviceProvider.GetService(typeof(IEnumerable<IBaseSettingsStorageBackend>)).Returns(Array.Empty<IBaseSettingsStorageBackend>());
        }

        if (overrideInformation is not null)
        {
            serviceProvider.GetService(typeof(IEnumerable<SettingsOverrideInformation>)).Returns(new[]
            {
                overrideInformation,
            });
        }
        else
        {
            serviceProvider.GetService(typeof(IEnumerable<SettingsOverrideInformation>)).Returns(Array.Empty<SettingsOverrideInformation>());
        }

        serviceProvider.GetService(typeof(IEnumerable<SettingsTypeInformation>)).Returns(new SettingsTypeInformation[]{
            new(
                ObjectType: typeof(MySettings),
                DefaultValue: new MySettings(),
                ConfigureLambda: MySettings.Configure
            ),
        });

        serviceProvider.GetService(typeof(IEnumerable<SettingsSectionSetup>)).Returns(Array.Empty<SettingsSectionSetup>());

        var settingsManager = new SettingsManager(serviceProvider);
        return settingsManager;
    }

    [Fact]
    public void TestSetup()
    {
        var settingsManager = Setup();
        settingsManager.Should().NotBeNull();
    }

    private record MySettings : ISettings
    {
        public bool Foo { get; set; }

        public string Bar { get; set; } = "";

        public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
        {
            return settingsBuilder.ConfigureDefault(ConfigureDefault);
        }

        private static MySettings ConfigureDefault(IServiceProvider serviceProvider)
        {
            return new MySettings
            {
                Bar = nameof(Bar),
            };
        }
    }

    private sealed class InMemoryStorageBackend : ISettingsStorageBackend
    {
        private readonly Dictionary<Type, object> _values = new();

        public SettingsStorageBackendId Id => SettingsStorageBackendId.DefaultValue;

        public void Save<T>(T value) where T : class, ISettings, new()
        {
            _values[typeof(T)] = value;
        }

        public T? Load<T>() where T : class, ISettings, new()
        {
            var value = _values.GetValueOrDefault(typeof(T));
            if (value is not T actual) return null;
            return actual;
        }

        public void Dispose() { }
    }
}
