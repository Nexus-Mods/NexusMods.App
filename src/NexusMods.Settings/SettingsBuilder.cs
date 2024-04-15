using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsBuilder : ISettingsBuilder
{
    public Func<IServiceProvider, object>? DefaultValueFactory { get; set; }
    public IStorageBackendBuilderValues? StorageBackendBuilderValues { get; set; }

    public ISettingsBuilder AddToUI<TSettings>(
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>.IFinishedStep> configureUI
    ) where TSettings : class, ISettings, new()
    {
        // TODO: implement this
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

    public ISettingsBuilder ConfigureStorageBackend<TSettings>(
        Action<ISettingsStorageBackendBuilder<TSettings>> configureStorageBackend)
        where TSettings : class, ISettings, new()
    {
        var builder = new SettingsStorageBackendBuilder<TSettings>();
        configureStorageBackend(builder);

        StorageBackendBuilderValues = builder;
        return this;
    }

    internal void Reset()
    {
        DefaultValueFactory = null;
        StorageBackendBuilderValues = null;
    }
}

internal interface IStorageBackendBuilderValues
{
    public SettingsStorageBackendId BackendId { get; }
    public Type? BackendType { get; }
}

internal class SettingsStorageBackendBuilder<T> : IStorageBackendBuilderValues, ISettingsStorageBackendBuilder<T>
    where T : class, ISettings, new()
{
    public SettingsStorageBackendId BackendId { get; private set; } = SettingsStorageBackendId.DefaultValue;
    public Type? BackendType { get; private set; } = null;

    public ISettingsStorageBackendBuilder<T> UseStorageBackend(SettingsStorageBackendId id)
    {
        BackendId = id;
        return this;
    }

    public ISettingsStorageBackendBuilder<T> UseStorageBackend<TBackend>() where TBackend : IBaseSettingsStorageBackend
    {
        BackendType = typeof(TBackend);
        return this;
    }
}
