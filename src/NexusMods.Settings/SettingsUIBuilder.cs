using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;
using UpdateAction = Action<Delegate, ISettingsManager, object>;

internal record PropertyBuilderOutput(
    SectionId SectionId,
    string DisplayName,
    string Description,
    bool RequiresRestart,
    string? RestartMessage,
    Delegate PropertySetterDelegate,
    UpdateAction UpdateAction,
    Func<ISettingsManager, object> GetValueFunc,
    ISettingsPropertyValueContainerFactory Factory
);

internal class SettingsUIBuilder<TSettings> : ISettingsUIBuilder<TSettings>
    where TSettings : class, ISettings, new()
{
    public List<PropertyBuilderOutput> PropertyBuilderOutputs { get; } = [];

    public ISettingsUIBuilder<TSettings> AddPropertyToUI<TProperty>(
        Expression<Func<TSettings, TProperty>> selectProperty,
        Func<IPropertyUIBuilder<TSettings, TProperty>, IPropertyUIBuilder<TSettings, TProperty>.IFinishedStep> configureProperty)
    {
        var builder = new PropertyUIBuilder<TSettings, TProperty>();
        _ = configureProperty(builder);

        if (selectProperty.Body is not MemberExpression memberExpression)
            throw new ArgumentException($"Expression `{selectProperty.Body}` is not a {nameof(MemberExpression)}");
        if (memberExpression.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException($"Member `{memberExpression.Member}` is not a {nameof(PropertyInfo)}");
        if (propertyInfo.GetSetMethod() is not { } methodInfo)
            throw new ArgumentException($"Method `{propertyInfo.GetSetMethod()}` is null!");

        // void Set_Property(TSettings this, TProperty newValue)
        var setDelegateType = Expression.GetDelegateType([typeof(TSettings), typeof(TProperty), typeof(void)]);

        var setDelegate = methodInfo.CreateDelegate(setDelegateType);
        var updateAction = CreateUpdateAction<TProperty>();

        var getValueFunc = CreateGetValueFunc(selectProperty);

        var output = builder.ToOutput(setDelegate, updateAction, getValueFunc);
        PropertyBuilderOutputs.Add(output);

        return this;
    }

    private static UpdateAction CreateUpdateAction<TProperty>()
    {
        return (propertySetterDelegate, settingsManager, newValue) =>
        {
            Debug.Assert(newValue.GetType() == typeof(TProperty));

            settingsManager.Update<TSettings>(settings =>
            {
                Debug.Assert(settings.GetType() == typeof(TSettings));

                propertySetterDelegate.DynamicInvoke([settings, newValue]);
                return settings;
            });
        };
    }

    private static Func<ISettingsManager, object> CreateGetValueFunc<TProperty>(
        Expression<Func<TSettings, TProperty>> selectProperty)
    {
        var func = selectProperty.Compile();

        return settingsManager =>
        {
            var settings = settingsManager.Get<TSettings>();

            var value = func.Invoke(settings);
            return value!;
        };
    }
}

internal class PropertyUIBuilder<TSettings, TProperty> :
    IPropertyUIBuilder<TSettings, TProperty>,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDisplayNameStep,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDescriptionStep,
    IPropertyUIBuilder<TSettings, TProperty>.IConfigureValueContainerStep,
    IPropertyUIBuilder<TSettings, TProperty>.IRequiresRestartStep
    where TSettings : class, ISettings, new()
{
    private SectionId _sectionId = SectionId.DefaultValue;
    private string _displayName = string.Empty;
    private string _description = string.Empty;
    private bool _requiresRestart;
    private string? _restartMessage;

    private ISettingsPropertyValueContainerFactory? _factory;

    internal PropertyBuilderOutput ToOutput(
        Delegate propertySetterDelegate,
        UpdateAction updateAction,
        Func<ISettingsManager, object> getValueFunc) => new(
        _sectionId,
        _displayName,
        _description,
        _requiresRestart,
        _restartMessage,
        propertySetterDelegate,
        updateAction,
        getValueFunc,
        _factory!
    );

    public IPropertyUIBuilder<TSettings, TProperty>.IWithDisplayNameStep AddToSection(SectionId id)
    {
        _sectionId = id;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IWithDescriptionStep WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IConfigureValueContainerStep WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IFinishedStep RequiresRestart(string message)
    {
        _requiresRestart = true;
        _restartMessage = message;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IFinishedStep RequiresRestart()
    {
        _requiresRestart = true;
        _restartMessage = null;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IRequiresRestartStep UseBooleanContainer()
    {
        _factory = new BooleanContainerFactory();
        return this;
    }

    private class BooleanContainerFactory : ISettingsPropertyValueContainerFactory
    {
        public SettingsPropertyValueContainer Create(object currentValue)
        {
            Debug.Assert(currentValue is bool);
            return new SettingsPropertyValueContainer(new BooleanContainer((bool)currentValue));
        }
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IRequiresRestartStep UseSingleValueMultipleChoiceContainer(
        IEqualityComparer<TProperty> valueComparer,
        TProperty[] allowedValues,
        Func<TProperty, string> valueToTranslation)
    {
        _factory = new SingleValueMultipleChoiceContainerFactory(
            valueComparer: EqualityComparer<object>.Create((a, b) =>
            {
                Debug.Assert(a is TProperty);
                Debug.Assert(b is TProperty);

                return valueComparer.Equals((TProperty)a, (TProperty)b);
            }),
            allowedValues,
            valueToTranslation
        );

        return this;
    }

    private class SingleValueMultipleChoiceContainerFactory : ISettingsPropertyValueContainerFactory
    {
        private readonly IEqualityComparer<object> _valueComparer;
        private readonly TProperty[] _allowedValues;
        private readonly Func<TProperty, string> _valueToTranslation;

        public SingleValueMultipleChoiceContainerFactory(
            IEqualityComparer<object> valueComparer,
            TProperty[] allowedValues,
            Func<TProperty, string> valueToTranslation)
        {
            _valueComparer = valueComparer;
            _allowedValues = allowedValues;
            _valueToTranslation = valueToTranslation;
        }

        public SettingsPropertyValueContainer Create(object currentValue)
        {
            Debug.Assert(currentValue is TProperty);

            var allowedValues = _allowedValues.Cast<object>().ToArray();

            return new SettingsPropertyValueContainer(new SingleValueMultipleChoiceContainer(
                currentValue,
                _valueComparer,
                allowedValues,
                ValueToTranslation
            ));

            string ValueToTranslation(object obj)
            {
                Debug.Assert(obj is TProperty);
                return _valueToTranslation((TProperty)obj);
            }
        }
    }
}

internal interface ISettingsPropertyValueContainerFactory
{
    SettingsPropertyValueContainer Create(object currentValue);
}
