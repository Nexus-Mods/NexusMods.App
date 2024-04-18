using System.Linq.Expressions;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal class SettingsUIBuilder<TSettings> : ISettingsUIBuilder<TSettings>
    where TSettings : class, ISettings, new()
{
    public List<PropertyBuilderOutput> PropertyBuilderOutputs { get; } = new();

    public ISettingsUIBuilder<TSettings> AddPropertyToUI<TProperty>(
        Expression<Func<TSettings, TProperty>> selectProperty,
        Func<IPropertyUIBuilder<TSettings, TProperty>, IPropertyUIBuilder<TSettings, TProperty>.IFinishedStep> configureProperty)
    {
        // TODO:
        var builder = new PropertyUIBuilder<TSettings, TProperty>();
        var done = configureProperty(builder);

        var output = builder.GetOutput();
        PropertyBuilderOutputs.Add(output);

        return this;
    }
}

internal class PropertyUIBuilder<TSettings, TProperty> :
    IPropertyUIBuilder<TSettings, TProperty>,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDisplayNameStep,
    IPropertyUIBuilder<TSettings, TProperty>.IWithDescriptionStep,
    IPropertyUIBuilder<TSettings, TProperty>.IOptionalStep
    where TSettings : class, ISettings, new()
{
    private SectionId _sectionId = SectionId.DefaultValue;
    private string _displayName = string.Empty;
    private string _description = string.Empty;
    private bool _requiresRestart;
    private string? _restartMessage;

    internal PropertyBuilderOutput GetOutput() => new(
        _sectionId,
        _displayName,
        _description,
        _requiresRestart,
        _restartMessage
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

    public IPropertyUIBuilder<TSettings, TProperty>.IOptionalStep WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IOptionalStep RequiresRestart(string message)
    {
        _requiresRestart = true;
        _restartMessage = message;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IOptionalStep RequiresRestart()
    {
        _requiresRestart = true;
        _restartMessage = null;
        return this;
    }

    public IPropertyUIBuilder<TSettings, TProperty>.IOptionalStep WithValidation(Func<TProperty, ValidationResult> validator)
    {
        // TODO:
        return this;
    }
}

internal record PropertyBuilderOutput(
    SectionId SectionId,
    string DisplayName,
    string Description,
    bool RequiresRestart,
    string? RestartMessage
);
