using System.Collections.Frozen;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.Settings;
using R3;

namespace NexusMods.Backend;

internal class SettingsManager : ISettingsManager
{
    private static readonly IScheduler Scheduler = TaskPoolScheduler.Default;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    private readonly Subject<(SettingKey, object)> _subject = new();
    private readonly Dictionary<SettingKey, object> _values = new();

    private readonly FrozenDictionary<Type, Func<object, object>> _overrides;

    private readonly FrozenDictionary<Type, IStorageBackend> _storageBackendMappings;
    private readonly FrozenDictionary<Type, IAsyncStorageBackend> _asyncStorageBackendMappings;

    public FrozenDictionary<Type, SettingsConfig> Configs { get; }

    public SettingsManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();

        var storageBackends = serviceProvider.GetServices<IBaseStorageBackend>().DistinctBy(x => x.Id).ToDictionary(x => x.Id, x => x);
        var defaultStorageBackend = serviceProvider.GetService<DefaultStorageBackend>()?.Backend;

        var settingsRegistrations = serviceProvider.GetServices<SettingsRegistration>().ToArray();
        var configs = new List<SettingsConfig>(capacity: settingsRegistrations.Length);

        var storageBackendMappings = new Dictionary<Type, IStorageBackend>();
        var asyncStorageBackendMappings = new Dictionary<Type, IAsyncStorageBackend>();

        foreach (var settingsRegistration in settingsRegistrations)
        {
            var settingsBuilder = new SettingsBuilder();

            try
            {
                settingsRegistration.ConfigureLambda.Invoke(settingsBuilder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to configure setting `{Type}`", settingsRegistration.ObjectType);
                continue;
            }

            var settingsConfig = settingsBuilder.ToConfig(settingsRegistration);
            configs.Add(settingsConfig);

            var storageBackendOptions = settingsConfig.StorageBackendOptions;
            if (storageBackendOptions is null)
            {
                if (defaultStorageBackend is IStorageBackend storageBackend) storageBackendMappings[settingsConfig.Type] = storageBackend;
                else if (defaultStorageBackend is IAsyncStorageBackend asyncStorageBackend) asyncStorageBackendMappings[settingsConfig.Type] = asyncStorageBackend;
            }
            else
            {
                if (storageBackendOptions.IsDisabled) continue;
                if (!storageBackends.TryGetValue(storageBackendOptions.Id, out var baseStorageBackend)) continue;

                if (baseStorageBackend is IStorageBackend storageBackend) storageBackendMappings[settingsConfig.Type] = storageBackend;
                else if (baseStorageBackend is IAsyncStorageBackend asyncStorageBackend) asyncStorageBackendMappings[settingsConfig.Type] = asyncStorageBackend;
            }
        }

        Configs = configs.ToFrozenDictionary(x => x.Type, x => x);
        _overrides = serviceProvider.GetServices<OverrideHack>().ToFrozenDictionary(x => x.SettingsType, x => x.Method);
        _storageBackendMappings = storageBackendMappings.ToFrozenDictionary();
        _asyncStorageBackendMappings = asyncStorageBackendMappings.ToFrozenDictionary();
    }

    private void CoreSet<T>(T value, SettingKey settingKey, bool notify) where T : class, ISettings, new()
    {
        _values[settingKey] = value;
        if (!notify) return;

        _subject.OnNext((settingKey, value));
        Save(value, settingKey.Key);
    }
 
    public void Set<T>(T value, string? key) where T : class, ISettings, new() => CoreSet(value, new SettingKey(typeof(T), key), notify: true);

    public T Get<T>(string? key) where T : class, ISettings, new()
    {
        var settingKey = new SettingKey(typeof(T), key);
        
        if (_values.TryGetValue(settingKey, out var obj))
        {
            Debug.Assert(obj is T);
            return (obj as T)!;
        }

        var savedValue = Load<T>(key);
        if (savedValue is not null)
        {
            savedValue = Override(savedValue, out _);
            CoreSet(savedValue, settingKey, notify: false);
            return savedValue;
        }

        var defaultValue = GetDefault<T>();
        defaultValue = Override(defaultValue, out var didOverride);
        CoreSet(defaultValue, settingKey, notify: !didOverride);

        return defaultValue;

        // ReSharper disable once VariableHidesOuterVariable
        T Override(T value, out bool didOverride)
        {
            didOverride = false;
            if (!_overrides.TryGetValue(typeof(T), out var overrideMethod)) return value;

            var overriden = overrideMethod.Invoke(value);
            Debug.Assert(overriden.GetType() == typeof(T));

            didOverride = true;
            return (T)overriden;
        }
    }

    public T GetDefault<T>() where T : class, ISettings, new()
    {
        var value = GetDefault(typeof(T));
        Debug.Assert(value is T);

        var res = (value as T)!;
        return res;
    }

    private object GetDefault(Type settingsType)
    {
        if (!Configs.TryGetValue(settingsType, out var config))
            throw new KeyNotFoundException($"Unknown settings type `{settingsType}`. Did you forget to register the setting with DI?");

        var defaultValue = config.DefaultValueFactory.Invoke(_serviceProvider);
        Debug.Assert(defaultValue.GetType() == settingsType, $"{defaultValue.GetType()} != {settingsType}");

        return defaultValue;
    }

    public T Update<T>(Func<T, T> updater, string? key) where T : class, ISettings, new()
    {
        var currentValue = Get<T>(key);
        var newValue = updater(currentValue);
        Set(newValue, key);

        return newValue;
    }

    public Observable<T> GetChanges<T>(string? key, bool prependCurrent = false) where T : class, ISettings, new()
    {
        var settingKey = new SettingKey(typeof(T), key);
        
        var result = _subject
            .Where(settingKey, static (tuple, state) => tuple.Item1 == state)
            .Select(tuple => (tuple.Item2 as T)!);

        return prependCurrent ? result.Prepend(Get<T>(key)) : result;
    }

#region Save/Load

    private void Save<T>(T value, string? key) where T : class, ISettings, new()
    {
        var type = typeof(T);

        if (_storageBackendMappings.TryGetValue(type, out var storageBackend))
        {
            try
            {
                storageBackend.Save(value, key);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while saving settings type `{Type}` with storage backend `{StorageBackendType}`", type, storageBackend.GetType());
            }
        } else if (_asyncStorageBackendMappings.TryGetValue(type, out var asyncStorageBackend))
        {
            Scheduler.ScheduleAsync((value, key, asyncStorageBackend, _logger), TimeSpan.Zero, async static (_, state, cancellationToken) =>
            {
                var (valueToSave, stringKey, backend, logger) = state;

                try
                {
                    await backend.Save(valueToSave, stringKey, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception while saving settings type `{Type}` with async storage backend `{AsyncStorageBackendType}`", valueToSave.GetType(), backend.GetType());
                }
            });
        }
    }

    private T? Load<T>(string? key) where T : class, ISettings, new()
    {
        var type = typeof(T);

        if (_storageBackendMappings.TryGetValue(type, out var storageBackend))
        {
            try
            {
                return storageBackend.Load<T>(key);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while loading settings type `{Type}` with storage backend `{StorageBackendType}`", type, storageBackend.GetType());
            }
        } else if (_asyncStorageBackendMappings.TryGetValue(type, out var asyncStorageBackend))
        {
            var waitHandle = new ManualResetEventSlim(initialState: false);
            T? res = null;

            var cts = new CancellationTokenSource(delay: TimeSpan.FromSeconds(10));

            _ = Task.Run(async () =>
            {
                try
                {
                    res = await asyncStorageBackend.Load<T>(key, cts.Token);
                    waitHandle.Set();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while loading settings type `{Type}` with async storage backend `{AsyncStorageBackendType}`", type, asyncStorageBackend.GetType());
                }
            }, cts.Token);

            try
            {
                if (!waitHandle.Wait(TimeSpan.FromSeconds(15), cts.Token))
                {
                    _logger.LogWarning("WaitHandle wasn't set after timeout!");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while waiting for task to complete");
            }

            return res;
        }

        return null;
    }
#endregion
}
