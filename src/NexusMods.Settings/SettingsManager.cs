using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal partial class SettingsManager : ISettingsManager
{
    private static readonly IScheduler Scheduler = TaskPoolScheduler.Default;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    private readonly Subject<(Type, object)> _subject = new();
    private readonly Dictionary<Type, object> _values = new();
    private readonly ImmutableDictionary<Type, ObjectCreationInformation> _objectCreationMappings;
    private readonly ImmutableDictionary<Type, Func<object,object>> _overrides;

    private readonly ImmutableDictionary<Type, ISettingsStorageBackend> _storageBackendMappings;
    private readonly ImmutableDictionary<Type, IAsyncSettingsStorageBackend> _asyncStorageBackendMappings;

    public SettingsManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();

        // overrides for tests
        _overrides = serviceProvider
            .GetServices<SettingsOverrideInformation>()
            .ToImmutableDictionary(x => x.Type, x => x.OverrideMethod);

        var baseStorageBackendArray = serviceProvider.GetServices<IBaseSettingsStorageBackend>().ToArray();
        var settingsTypeInformationArray = serviceProvider.GetServices<SettingsTypeInformation>().ToArray();
        var defaultBaseStorageBackend = serviceProvider.GetService<DefaultSettingsStorageBackend>()?.Backend;

        var builderOutput = Setup(_logger, settingsTypeInformationArray, baseStorageBackendArray, defaultBaseStorageBackend);
        _objectCreationMappings = builderOutput.ObjectCreationMappings;
        _storageBackendMappings = builderOutput.StorageBackendMappings;
        _asyncStorageBackendMappings = builderOutput.AsyncStorageBackendMappings;
    }

    private void CoreSet<T>(T value, bool notify) where T : class, ISettings, new()
    {
        var type = typeof(T);
        _values[type] = value;
        if (!notify) return;

        _subject.OnNext((type, value));
        Save(value);
    }

    public void Set<T>(T value) where T : class, ISettings, new() => CoreSet(value, notify: true);

    public T Get<T>() where T : class, ISettings, new()
    {
        if (_values.TryGetValue(typeof(T), out var obj)) return (obj as T)!;

        var savedValue = Load<T>();
        if (savedValue is not null)
        {
            CoreSet(savedValue, notify: false);
            return savedValue;
        }

        var defaultValue = GetDefaultValue<T>();
        Set(defaultValue);

        return defaultValue;
    }

    public T Update<T>(Func<T, T> updater) where T : class, ISettings, new()
    {
        var currentValue = Get<T>();
        var newValue = updater(currentValue);
        Set(newValue);

        return newValue;
    }

    public IObservable<T> GetChanges<T>() where T : class, ISettings, new()
    {
        return _subject
            .Where(tuple => tuple.Item1 == typeof(T))
            .Select(tuple => (tuple.Item2 as T)!);
    }

    private T GetDefaultValue<T>() where T : class, ISettings, new()
    {
        if (!_objectCreationMappings.TryGetValue(typeof(T), out var objectCreationInformation))
            throw new KeyNotFoundException($"Unknown settings type '{typeof(T)}'. Did you forget to register the setting with DI?");

        var value = objectCreationInformation.GetOrCreateDefaultValue(_serviceProvider);

        if (_overrides.TryGetValue(typeof(T), out var overrideMethod))
        {
            value = overrideMethod.Invoke(value);
        }

        var res = (value as T)!;
        return res;
    }

    private void Save<T>(T value) where T : class, ISettings, new()
    {
        var type = typeof(T);

        if (_storageBackendMappings.TryGetValue(type, out var storageBackend))
        {
            try
            {
                storageBackend.Save(value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while saving settings type `{Type}` with storage backend `{StorageBackendType}`", type, storageBackend.GetType());
            }
        } else if (_asyncStorageBackendMappings.TryGetValue(type, out var asyncStorageBackend))
        {
            Scheduler.ScheduleAsync((value, asyncStorageBackend, _logger), TimeSpan.Zero, async static (scheduler, state, cancellationToken) =>
            {
                var (valueToSave, backend, logger) = state;

                try
                {
                    await backend.Save(valueToSave, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception while saving settings type `{Type}` with async storage backend `{AsyncStorageBackendType}`", valueToSave.GetType(), backend.GetType());
                }
            });
        }
    }

    private T? Load<T>() where T : class, ISettings, new()
    {
        var type = typeof(T);

        if (_storageBackendMappings.TryGetValue(type, out var storageBackend))
        {
            try
            {
                return storageBackend.Load<T>();
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

            var proxy = Task.Run(async () =>
            {
                try
                {
                    res = await asyncStorageBackend.Load<T>(cts.Token);
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
                    _logger.LogWarning("WaitHandle wasn't set after timout!");
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
}
