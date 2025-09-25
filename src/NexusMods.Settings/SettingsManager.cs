using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Sdk;

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

    private readonly IPropertyBuilderOutput[] _propertyBuilderOutputs;
    private readonly Lazy<ISettingsSectionDescriptor[]> _sectionDescriptors;

    public SettingsManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();

        // overrides for tests
        _overrides = serviceProvider
            .GetServices<SettingsOverrideInformation>()
            .ToImmutableDictionary(x => x.Type, x => x.OverrideMethod);

        var settingsSectionSetups = serviceProvider.GetServices<SettingsSectionSetup>().ToArray();

        // NOTE(erri120): This has to be Lazy because icons aren't available until Avalonia starts up.
        _sectionDescriptors = new Lazy<ISettingsSectionDescriptor[]>(() => settingsSectionSetups
            .Select(descriptor => (ISettingsSectionDescriptor)new SettingsSectionDescriptor
            {
                Id = descriptor.Id,
                Name = descriptor.Name,
                Icon = descriptor.IconFunc(),
                Priority = descriptor.Priority,
                Hidden = descriptor.Hidden,
            })
            .ToArray(),
            mode: LazyThreadSafetyMode.ExecutionAndPublication
        );

        var baseStorageBackendArray = serviceProvider.GetServices<IBaseSettingsStorageBackend>().ToArray();
        var settingsTypeInformationArray = serviceProvider.GetServices<SettingsTypeInformation>().ToArray();
        var defaultBaseStorageBackend = serviceProvider.GetService<DefaultSettingsStorageBackend>()?.Backend;

        var builderOutput = Setup(_logger, settingsTypeInformationArray, baseStorageBackendArray, defaultBaseStorageBackend);
        _objectCreationMappings = builderOutput.ObjectCreationMappings;
        _storageBackendMappings = builderOutput.StorageBackendMappings;
        _asyncStorageBackendMappings = builderOutput.AsyncStorageBackendMappings;
        _propertyBuilderOutputs = builderOutput.PropertyBuilderOutputs;

        if (ApplicationConstants.IsDebug)
        {
            var ids = new HashSet<SectionId>();
            foreach (var sectionDescriptor in settingsSectionSetups)
            {
                var id = sectionDescriptor.Id;
                Debug.Assert(ids.Add(id), $"duplicate section ID: {id}");
            }

            foreach (var propertyBuilderOutput in _propertyBuilderOutputs)
            {
                Debug.Assert(settingsSectionSetups.Any(x => x.Id == propertyBuilderOutput.SectionId), $"section not registered: {propertyBuilderOutput.SectionId} (for setting {propertyBuilderOutput.DisplayName})");
            }
        }
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
        var settingsType = typeof(T);
        if (_values.TryGetValue(typeof(T), out var obj)) return (obj as T)!;

        var savedValue = Load<T>();
        if (savedValue is not null)
        {
            savedValue = Override(savedValue, out _);
            CoreSet(savedValue, notify: false);
            return savedValue;
        }

        var defaultValue = GetDefaultValue<T>();
        defaultValue = Override(defaultValue, out var didOverride);
        CoreSet(defaultValue, notify: !didOverride);

        return defaultValue;

        // ReSharper disable once VariableHidesOuterVariable
        T Override(T value, out bool didOverride)
        {
            didOverride = false;
            if (!_overrides.TryGetValue(settingsType, out var overrideMethod)) return value;

            var overriden = overrideMethod.Invoke(value);
            Debug.Assert(overriden.GetType() == settingsType);

            didOverride = true;
            return (T)overriden;
        }
    }

    public T Update<T>(Func<T, T> updater) where T : class, ISettings, new()
    {
        var currentValue = Get<T>();
        var newValue = updater(currentValue);
        Set(newValue);

        return newValue;
    }

    public IObservable<T> GetChanges<T>(bool prependCurrent = false) where T : class, ISettings, new()
    {
        var result = _subject
            .Where(tuple => tuple.Item1 == typeof(T))
            .Select(tuple => (tuple.Item2 as T)!);
        return prependCurrent ? result.Prepend(Get<T>()) : result;
    }

    public ISettingsPropertyUIDescriptor[] GetAllUIProperties()
    {
        // ReSharper disable once CoVariantArrayConversion
        return _propertyBuilderOutputs.Select(output =>
        {
            var value = output.GetValue(this);
            var defaultValue = output.GetDefaultValue(this);

            var valueContainer = output.Factory.Create(value, defaultValue, output);
            return SettingsPropertyUIDescriptor.From(output, valueContainer);
        }).ToArray();
    }

    public ISettingsSectionDescriptor[] GetAllSections() => _sectionDescriptors.Value;

    private object GetDefaultValue(Type settingsType)
    {
        if (!_objectCreationMappings.TryGetValue(settingsType, out var objectCreationInformation))
            throw new KeyNotFoundException($"Unknown settings type '{settingsType}'. Did you forget to register the setting with DI?");

        var value = objectCreationInformation.GetOrCreateDefaultValue(_serviceProvider);
        return value;
    }

    internal T GetDefaultValue<T>() where T : class, ISettings, new()
    {
        var value = GetDefaultValue(typeof(T));
        Debug.Assert(value.GetType() == typeof(T));

        var res = (value as T)!;
        return res;
    }

#region Save/Load

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
            Scheduler.ScheduleAsync((value, asyncStorageBackend, _logger), TimeSpan.Zero, async static (_, state, cancellationToken) =>
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

            _ = Task.Run(async () =>
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
#endregion
}
