using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsManager : ISettingsManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    private readonly Subject<(Type, object)> _subject = new();
    private readonly Dictionary<Type, object> _values = new();
    private readonly ImmutableDictionary<Type,ObjectCreationInformation> _objectCreationDict;
    private readonly ImmutableDictionary<Type,Func<object,object>> _overrides;

    public SettingsManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();

        var builder = new SettingsBuilder();

        _overrides = serviceProvider
            .GetServices<SettingsOverrideInformation>()
            .ToImmutableDictionary(x => x.Type, x => x.OverrideMethod);

        _objectCreationDict = serviceProvider
            .GetServices<SettingsTypeInformation>()
            .Select(information =>
            {
                try
                {
                    information.ConfigureLambda.Invoke(builder);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while configuring {Type}", information.ObjectType);
                }

                var defaultValueFactory = builder.DefaultValueFactory;
                builder.Reset();

                return new ObjectCreationInformation(information.ObjectType, information.DefaultValue, defaultValueFactory);
            })
            .ToImmutableDictionary(x => x.ObjectType, x => x);
    }

    public void Set<T>(T value) where T : class, ISettings, new()
    {
        _values[typeof(T)] = value;
        _subject.OnNext((typeof(T), value));
    }

    private T GetDefaultValue<T>() where T : class, ISettings, new()
    {
        if (!_objectCreationDict.TryGetValue(typeof(T), out var objectCreationInformation))
            throw new KeyNotFoundException($"Unknown settings type '{typeof(T)}'. Did you forget to register the setting with DI?");

        var value = objectCreationInformation.GetOrCreateDefaultValue(_serviceProvider);

        if (_overrides.TryGetValue(typeof(T), out var overrideMethod))
        {
            value = overrideMethod.Invoke(value);
        }

        var res = (value as T)!;
        return res;
    }

    public T Get<T>() where T : class, ISettings, new()
    {
        if (_values.TryGetValue(typeof(T), out var obj)) return (obj as T)!;

        var value = GetDefaultValue<T>();
        Set(value);

        return value;
    }

    public IObservable<T> GetChanges<T>() where T : class, ISettings, new()
    {
        return _subject
            .Where(tuple => tuple.Item1 == typeof(T))
            .Select(tuple => (tuple.Item2 as T)!);
    }
}
