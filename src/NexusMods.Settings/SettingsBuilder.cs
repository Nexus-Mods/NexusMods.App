using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsBuilder : ISettingsBuilder
{
    public Func<IServiceProvider, object>? DefaultValueFactory { get; set; }

    public ISettingsBuilder AddToUI<TSettings>(
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>.IFinishedStep> configureUI
    ) where TSettings : class, ISettings, new()
    {
        return this;
    }

    public ISettingsBuilder ConfigureDefault<TSettings>(
        Func<IServiceProvider, TSettings> defaultValueFactory
    ) where TSettings : class, ISettings, new()
    {
        Func<IServiceProvider, object> hack = defaultValueFactory;

        DefaultValueFactory = hack;
        return this;
    }

    internal void Reset()
    {
        DefaultValueFactory = null;
    }
}
