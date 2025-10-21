using NexusMods.Sdk.Settings;
using R3;

// ReSharper disable All

namespace Examples.Settings;

file class MyCoolSettings : ISettings
{
    public string Name { get; init; } = "Some Name";

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // This settings class doesn't have to appear on the UI, so we don't
        // have to configure anything and can just return the provided builder:
        return settingsBuilder;
    }
}

file class MyService
{
    private readonly ISettingsManager _settingsManager;

    public MyService(ISettingsManager settingsManager)
    {
        // Get the ISettingsManager via DI:
        _settingsManager = settingsManager;
    }

    public void DoSomething()
    {
        // Call Get<TSettings> to get the current values for the provided type.
        // You shouldn't cache the current settings values, instead, you should
        // always call Get<TSettings>
        var name = _settingsManager.Get<MyCoolSettings>().Name;

        Console.WriteLine($"My name is '{name}'");
    }

    public void ReactToChanges()
    {
        // You can also react to changes to the settings. This is very useful
        // if you're already doing reactive work that depends on the settings.
        // This allows you to refresh data or re-trigger another observable stream.
        _settingsManager
            .GetChanges<MyCoolSettings>(prependCurrent: false)
            .Subscribe(newValue =>
            {
                Console.WriteLine($"Settings changed to {newValue}");
            });
    }
}
