using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

public class ObjectCreationInformation
{
    public Type ObjectType { get; }
    public ISettings DefaultValue { get; }
    public Func<IServiceProvider, object>? DefaultValueFactory { get; }

    public ObjectCreationInformation(Type objectType, ISettings defaultValue, Func<IServiceProvider, object>? defaultValueFactory)
    {
        ObjectType = objectType;
        DefaultValue = defaultValue;
        DefaultValueFactory = defaultValueFactory;
    }

    public object GetOrCreateDefaultValue(IServiceProvider serviceProvider)
    {
        return DefaultValueFactory is not null ? DefaultValueFactory(serviceProvider) : DefaultValue;
    }
}
