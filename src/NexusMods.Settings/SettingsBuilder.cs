using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsBuilder : ISettingsBuilder
{
    public Func<IServiceProvider, object>? DefaultValueFactory { get; private set; }
    public IStorageBackendBuilderValues? StorageBackendBuilderValues { get; private set; }
    public List<PropertyBuilderOutput> PropertyBuilderOutputs { get; private set; } = [];

    public ISettingsBuilder AddToUI<TSettings>(
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>> configureUI
    ) where TSettings : class, ISettings, new()
    {
        var builder = new SettingsUIBuilder<TSettings>();
        _ = configureUI(builder);

        PropertyBuilderOutputs = builder.PropertyBuilderOutputs;
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
        PropertyBuilderOutputs = [];
    }
}

internal interface IStorageBackendBuilderValues
{
    public SettingsStorageBackendId BackendId { get; }
    public Type? BackendType { get; }
    public bool IsDisabled { get; }
}

internal class SettingsStorageBackendBuilder<T> : IStorageBackendBuilderValues, ISettingsStorageBackendBuilder<T>
    where T : class, ISettings, new()
{
    public SettingsStorageBackendId BackendId { get; private set; } = SettingsStorageBackendId.DefaultValue;
    public Type? BackendType { get; private set; }
    public bool IsDisabled { get; private set; }

    public void Disable()
    {
        IsDisabled = true;
    }

    public void UseStorageBackend(SettingsStorageBackendId id)
    {
        BackendId = id;
    }

    public void UseStorageBackend<TBackend>() where TBackend : IBaseSettingsStorageBackend
    {
        BackendType = typeof(TBackend);
    }
}
