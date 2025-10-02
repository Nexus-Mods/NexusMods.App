using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

[PublicAPI]
public interface IInteractionControl : IViewModelInterface
{
    IPropertyValueContainer ValueContainer { get; }
}

[PublicAPI]
public interface IInteractionControlFactory<in TContainerOptions>
    where TContainerOptions : IContainerOptions
{
    IInteractionControl Create(IServiceProvider serviceProvider, ISettingsManager settingsManager, TContainerOptions containerOptions, PropertyConfig propertyConfig);
}
