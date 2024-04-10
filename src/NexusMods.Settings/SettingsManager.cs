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

    public SettingsManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();

        var builder = new SettingsBuilder();

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

    public T Get<T>() where T : class, ISettings, new()
    {
        if (_values.TryGetValue(typeof(T), out var obj)) return (obj as T)!;

        if (!_objectCreationDict.TryGetValue(typeof(T), out var objectCreationInformation))
            throw new KeyNotFoundException($"Unknown settings type '{typeof(T)}'. Did you forget to register the setting with DI?");

        var value = (objectCreationInformation.GetOrCreateDefaultValue(_serviceProvider) as T)!;
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
