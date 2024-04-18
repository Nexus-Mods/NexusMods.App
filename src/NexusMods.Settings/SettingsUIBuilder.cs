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
    UpdateAction UpdateAction
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
        var delegateType = Expression.GetDelegateType([typeof(TSettings), typeof(TProperty), typeof(void)]);
        // type erasure into Delegate
        var @delegate = methodInfo.CreateDelegate(delegateType);

        var updater = CreateUpdateAction<TProperty>();

        var output = builder.ToOutput(@delegate, updater);
        PropertyBuilderOutputs.Add(output);

        return this;
    }

    private static UpdateAction CreateUpdateAction<TProperty>()
    {
        return (propertySetterDelegate, settingsManager, newValue) =>
        {
            if (newValue.GetType() != typeof(TProperty))
                throw new ArgumentException($"Type mismatch: `{newValue.GetType()}` != `{typeof(TProperty)}`");

            settingsManager.Update<TSettings>(settings =>
            {
                propertySetterDelegate.DynamicInvoke([settings, newValue]);
                return settings;
            });
        };
    }
}

internal class PropertyUIBuilder<TSettings, TProperty> :
    IPropertyUIBuilder<TSettings, TProperty>,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDisplayNameStep,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDescriptionStep,
    IPropertyUIBuilder<TSettings, TProperty>.IRequiresRestartStep
    where TSettings : class, ISettings, new()
{
    private SectionId _sectionId = SectionId.DefaultValue;
    private string _displayName = string.Empty;
    private string _description = string.Empty;
    private bool _requiresRestart;
    private string? _restartMessage;

    internal PropertyBuilderOutput ToOutput(
        Delegate propertySetterDelegate,
        UpdateAction updateAction) => new(
        _sectionId,
        _displayName,
        _description,
        _requiresRestart,
        _restartMessage,
        propertySetterDelegate,
        updateAction
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

    public IPropertyUIBuilder<TSettings, TProperty>.IRequiresRestartStep WithDescription(string description)
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
}
