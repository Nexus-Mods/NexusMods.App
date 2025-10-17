using System.Linq.Expressions;
using System.Reflection;
using NexusMods.Sdk.Settings;

namespace NexusMods.Backend;

internal class SettingsBuilder : ISettingsBuilder
{
    private Func<IServiceProvider, object>? _defaultValueFactory;
    private StorageBackendOptions? _storageBackendOptions;
    private readonly List<PropertyConfig> _properties = [];

    public SettingsConfig ToConfig(SettingsRegistration settingsRegistration)
    {
        return new SettingsConfig(
            Type: settingsRegistration.ObjectType,
            DefaultValueFactory: _defaultValueFactory ?? (_ => settingsRegistration.DefaultValue),
            Properties: _properties.ToArray(),
            StorageBackendOptions: _storageBackendOptions
        );
    }

    public ISettingsBuilder ConfigureDefault<TSettings>(Func<IServiceProvider, TSettings> defaultValueFactory)
        where TSettings : class, ISettings, new()
    {
        _defaultValueFactory = defaultValueFactory;
        return this;
    }

    public ISettingsBuilder ConfigureBackend(StorageBackendOptions options)
    {
        _storageBackendOptions = options;
        return this;
    }

    public ISettingsBuilder ConfigureProperty<TSettings, TProperty>(
        Expression<Func<TSettings, TProperty>> propertySelector,
        PropertyOptions<TSettings, TProperty> options,
        IContainerOptions? containerOptions)
        where TSettings : class, ISettings, new()
        where TProperty : notnull
    {
        if (propertySelector.Body is not MemberExpression memberExpression)
            throw new ArgumentException($"Expression `{propertySelector.Body}` is not a {nameof(MemberExpression)}");
        if (memberExpression.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException($"Member `{memberExpression.Member}` is not a {nameof(PropertyInfo)}. Settings only support properties, not {memberExpression.Member.GetType()}");
        if (propertyInfo.GetSetMethod() is not { } methodInfo)
            throw new ArgumentException($"Property `{propertyInfo}` doesn't have a setter!");

        // void Set_Property(TSettings this, TProperty newValue)
        var setDelegateType = Expression.GetDelegateType([typeof(TSettings), typeof(TProperty), typeof(void)]);
        var setDelegate = methodInfo.CreateDelegate(setDelegateType);
        var compiledFunc = propertySelector.Compile();

        var wrapper = new PropertyOptionsWrapper
        {
            Section = options.Section,
            DisplayName = options.DisplayName,
            RequiresRestart = options.RequiresRestart,
            RestartMessage = options.RestartMessage,
            HelpLink = options.HelpLink,
            DescriptionFactory = obj => options.DescriptionFactory(Cast<TSettings, TProperty>(obj)),
            Validation = options.Validation is null ? null : obj => options.Validation(Cast<TSettings, TProperty>(obj)),
        };

        _properties.Add(new PropertyConfig(
            Options: wrapper,
            ContainerOptions: containerOptions,
            GetValue: GetValue,
            GetDefaultValue: GetDefaultValue,
            Update: Update
        ));

        return this;
        object GetValue(ISettingsManager settingsManager)
        {
            var settings = settingsManager.Get<TSettings>();
            return compiledFunc.Invoke(settings);
        }

        object GetDefaultValue(ISettingsManager settingsManager)
        {
            var settings = settingsManager.GetDefault<TSettings>();
            return compiledFunc.Invoke(settings);
        }

        void Update(ISettingsManager settingsManager, object obj)
        {
            settingsManager.Update<TSettings>(settings =>
            {
                var newValue = Cast<TSettings, TProperty>(obj);

                // void Set_Property(TSettings this, TProperty newValue)
                setDelegate.DynamicInvoke([settings, newValue]);
                return settings;
            });
        }
    }

    private static TProperty Cast<TSettings, TProperty>(object obj)
        where TSettings : class, ISettings, new()
        where TProperty : notnull
    {
        if (obj is not TProperty value) throw new ArgumentException($"Input for {typeof(TSettings)} is not of type {typeof(TProperty)}", nameof(obj));
        return value;
    }
}
